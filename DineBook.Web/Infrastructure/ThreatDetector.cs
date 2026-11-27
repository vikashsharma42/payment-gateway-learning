using DineBook.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DineBook.Web.Infrastructure
{
    // Rules that spot common attacks in what a visitor sends. These are signals, not the main defence:
    // database access uses parameters (EF Core), Razor encodes output and form posts carry antiforgery tokens.
    public static class ThreatDetector
    {
        // Hidden form field that people never fill in. Bots that fill every field do.
        public const string HoneypotField = "website_url";

        private const int MaxScanLength = 2000;
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

        private static readonly Regex SqlInjection = new(
            @"\bunion\b(\s+all)?\s+select\b" +
            @"|'\s*(or|and)\s+['""\w]+\s*=" +
            @"|\bor\s+1\s*=\s*1\b" +
            @";\s*(drop|delete|insert|update|exec|shutdown|alter)\b" +
            @"|\bxp_cmdshell\b|\bwaitfor\s+delay\b|\bbenchmark\s*\(|\bsleep\s*\(\s*\d" +
            @"|'\s*--|\binformation_schema\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);

        private static readonly Regex Xss = new(
            @"<\s*/?\s*(script|iframe|object|embed|svg|img|body|meta|link|style)\b" +
            @"|javascript\s*:|\bon(error|load|click|mouseover|focus|toggle|begin|animationstart)\s*=" +
            @"|document\.(cookie|write)|\beval\s*\(|expression\s*\(",
            RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);

        private static readonly Regex Scanners = new(
            @"sqlmap|nikto|nmap|masscan|acunetix|nessus|dirbuster|gobuster|wpscan|havij|zgrab|nuclei",
            RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);

        private static readonly Regex Automation = new(
            @"python-requests|python-urllib|go-http-client|scrapy|\bcurl/|\bwget/|libwww|okhttp|headlesschrome|phantomjs|node-fetch|axios/",
            RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);

        // Paths that exist on other sites' software. This site never serves them.
        private static readonly string[] HoneypotPaths =
        {
            "/wp-admin", "/wp-login", "/xmlrpc.php", "/.env", "/.git", "/phpmyadmin", "/admin.php", "/config.php", "/server-status", "/actuator"
        };

        public static bool IsScanner(string userAgent) =>
            !string.IsNullOrEmpty(userAgent) && Scanners.IsMatch(userAgent);

        // Empty agents and scripting clients. Logged, not refused, because some are legitimate tools.
        public static bool IsAutomated(string userAgent) =>
            string.IsNullOrWhiteSpace(userAgent) || Automation.IsMatch(userAgent);

        public static bool IsHoneypot(string path) =>
            HoneypotPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        // Attacks found in the visitor's input. Each kind is reported once per request. Only field names are kept, not the payload.
        public static IReadOnlyList<(SecurityEventKind Kind, string Detail)> Inspect(IEnumerable<(string Name, string Value)> inputs)
        {
            var findings = new List<(SecurityEventKind Kind, string Detail)>();

            foreach (var (name, value) in inputs)
            {
                if (string.IsNullOrEmpty(value))
                    continue;

                var text = value.Length > MaxScanLength ? value[..MaxScanLength] : value;

                if (SqlInjection.IsMatch(text) && !findings.Exists(f => f.Kind == SecurityEventKind.SqlInjectionSuspected))
                    findings.Add((SecurityEventKind.SqlInjectionSuspected, $"SQL injection pattern in field '{name}'"));

                if (Xss.IsMatch(text) && !findings.Exists(f => f.Kind == SecurityEventKind.XssSuspected))
                    findings.Add((SecurityEventKind.XssSuspected, $"Script injection pattern in field '{name}'"));
            }

            return findings;
        }
    }
}