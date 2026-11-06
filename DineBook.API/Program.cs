using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using DineBook.API;
using System.IO;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseStartup<Startup>();
    })
    .Build();

host.Run();
