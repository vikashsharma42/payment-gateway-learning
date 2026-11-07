using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace DineBook.Web.Pages
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Entry point of the application
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            // Create and configure the default Host Builder
            return Host.CreateDefaultBuilder(args)
              .ConfigureAppConfiguration((hostingContext, config) =>
              {
                  // Set the base path for configuration files
                  var basePath = AppContext.BaseDirectory;
                  config.SetBasePath(basePath);

                  // Add a shared JSON settings file to the configuration
                  // config.AddJsonFile("sharedsettings.json", optional: false, reloadOnChange: true);
              })
              .ConfigureWebHostDefaults(webBuilder =>
              {
                  // Specify the Startup class to configure the application
                  webBuilder.UseStartup<Startup>();
              });
        }
    }
}
