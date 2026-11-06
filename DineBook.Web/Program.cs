using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using DineBook.Web;
using System.IO;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseWebRoot("wwwroot")
            .UseStartup<Startup>();
    })
    .Build();

host.Run();
