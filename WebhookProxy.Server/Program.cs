using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebhookProxy.Server.IO;

namespace WebhookProxy.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder(args)
                    .ConfigureServices((services) =>
                    {
                        services.AddCors(options =>
                        {
                            options.AddPolicy("*",
                                builder => builder.AllowAnyOrigin()
                                                  .AllowAnyHeader()
                                                  .AllowAnyMethod());
                        });

                        services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

                        services.AddHttpContextAccessor();
                        services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

                        services.AddSignalR();
                    })
                    .Configure((app) =>
                    {
                        //app.UseDeveloperExceptionPage();
                        //app.UseHsts();
                        app.UseHttpsRedirection();
                        
                        app.UseStaticFiles();

                        app.UseCors("*");

                        app.UseSignalR(routes => routes.MapHub<ProxyClientHub>("/proxy-client"));

                        app.UseMvc();
                        
                    })
                    .Build()
                    .Run();
        }
    }
}