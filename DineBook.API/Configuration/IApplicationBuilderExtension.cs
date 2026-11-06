using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using DineBook.API.Middleware;

namespace DineBook.API.Configuration
{
    public static class IApplicationBuilderExtension
    {
        public static void ConfigureDineBookMiddleware(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            // Single exception boundary: domain errors become 400, everything else 500
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (!environment.IsDevelopment())
            {
                app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseCors(IServiceCollectionExtension.CorsPolicy);
        }

        public static void ConfigureDineBookEndpoints(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                if (environment.IsDevelopment())
                {
                    endpoints.MapOpenApi();
                }
            });
        }
    }
}