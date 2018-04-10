using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Webplayer.Data;
using Fastnet.WebPlayer.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fastnet.Webplayer
{
    public class Startup
    {
        private readonly ILogger log;
        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            this.log = logger;
            log.Information("Web player started");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<MusicConfiguration>(Configuration.GetSection("MusicConfiguration"));
            services.Configure<PlayerConfiguration>(Configuration.GetSection("PlayerConfiguration"));
            services.AddWebDbContext<PlayerDb, PlayerDbContextFactory, PlayerDbOptions>(Configuration, "PlayerDbOptions");
            services.AddScheduler(Configuration);
            services.AddSingleton<Messenger>();

            services.AddSingleton<RealtimeTask, Receiver>();
            services.AddSingleton<RealtimeTask, Broadcaster>();
            services.AddSingleton<PocoStore>();
            // services.AddSingleton<RealtimeTask, RealtimeTester>();
            services.AddSingleton<DeviceManagerFactory>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IOptions<PlayerConfiguration> playerConfigOptions, IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            log.Trace($"{(env.IsDevelopment() ? "dev mode" : "prod mode")} player config {playerConfigOptions.Value.ToJson()}");
            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetService<PlayerDb>();
                    PlayerDbInitialiser.Initialise(db);
                }
                catch (System.Exception xe)
                {
                    log.Error(xe, $"Error initialising PlayerDb");
                }
            }
        }
    }
}
