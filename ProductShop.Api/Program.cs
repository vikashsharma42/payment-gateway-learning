using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using ProductShop.API;
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
