using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Services;
using Graylog2Grafana.Workers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using Prometheus;
using Serilog;
using StackExchange.Redis;
using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using Time.Series.Anomaly.Detection.Abstractions;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;
using Time.Series.Anomaly.Detection.Data.Services;
using Time.Series.Anomaly.Detection.Services;

namespace Graylog2Grafana.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                      .ReadFrom.Configuration(Configuration, "Serilog")
                      .CreateLogger();

            var filesDirectory = new DirectoryInfo(Configuration.GetValue<string>("Configuration:FilesPath"));

            if (!filesDirectory.Exists)
            {
                filesDirectory.Create();
            }

            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite($"Data Source={Path.Combine(filesDirectory.FullName, "Graylog2Grafana_v5.db")}");
            });

            var aspDataProtectionRedisHost = Configuration.GetValue<string>("Configuration:DataProtection:Redis:Host");
            if (!string.IsNullOrWhiteSpace(aspDataProtectionRedisHost))
            {
                var aspDataProtectionRedisPort = Configuration.GetValue<int?>("Configuration:DataProtection:Redis:Port") ?? 6379;

                var redisUri = $"{aspDataProtectionRedisHost}:{aspDataProtectionRedisPort}";

                Console.WriteLine($"Using redis to persist key for data protection: '{redisUri}'");
                var redis = ConnectionMultiplexer.Connect(redisUri);
                services.AddDataProtection()
                    .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");
            }

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = new PathString("/Account/Login");
                options.AccessDeniedPath = new PathString("/Account/Logout");
                options.LogoutPath = new PathString("/Account/Logout");
            });

            services
            .AddMemoryCache()
            .AddControllersWithViews()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });

            services
            // Singleton
            .AddSingleton<ILogger>((s) => Log.Logger)
            .AddSingleton<IDataService, GraylogDataService>()
            .AddSingleton<INotificationService, SlackNotificationService>()
            .AddSingleton<IAnomalyDetectionService, AnomalyDetectionService>()
            .AddSingleton<IMonitorSeriesDataAnomalyDetectionService, MonitorSeriesDataAnomalyDetectionService>()
            .AddSingleton<INotificationSlackService, NotificationSlackService>()
            .AddSingleton<IMonitorSourcesService, MonitorSourcesService>()
            .AddSingleton<IMonitorGroupsService, MonitorGroupsService>()            
            .AddSingleton<IMonitorSeriesService, MonitorSeriesService>()
            .AddSingleton<IMonitorSeriesDataService, MonitorSeriesDataService>()
            .AddSingleton<IAnomalyDetectionDataService, AnomalyDetectionDataService>()
            .AddSingleton<IGrafanaSimpleJsonPluginService, GrafanaSimpleJsonPluginService>()
            // Scoped
            .AddScoped<IUsersService, UsersService>()
            // Hosted services
            .AddHostedService<LoadDataWorker>()
            .AddHostedService<AnomalyDetectionWorker>();

            services.AddHttpClient("Graylog", c =>
            {
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                c.DefaultRequestHeaders.Add("X-Requested-By", "XMLHttpRequest");
                c.Timeout = TimeSpan.FromSeconds(10);
            });

            services.AddHttpClient("Slack", c =>
            {
                c.BaseAddress = new Uri("https://slack.com");
            });

            services.AddHealthChecks();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureCreated();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseSerilogRequestLogging();

            app.Use(async (context, next) => { context.Request.EnableBuffering(); await next(); });

            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin();
                builder.AllowAnyMethod();
                builder.AllowAnyHeader();
            });

            app.UseForwardedHeaders();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapHealthChecks("/healthz/liveness");
                endpoints.MapHealthChecks("/healthz/readiness");

                endpoints.MapMetrics();
            });
        }
    }
}
