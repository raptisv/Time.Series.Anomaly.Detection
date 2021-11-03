using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models.Configuration;
using Graylog2Grafana.Services;
using Graylog2Grafana.Workers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
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

            services
            .Configure<DatasetConfiguration>(Configuration.GetSection("Dataset"))
            .Configure<GraylogConfiguration>(Configuration.GetSection("Graylog"))
            .Configure<SlackConfiguration>(Configuration.GetSection("Slack"));

            var filesDirectory = new DirectoryInfo(Configuration.GetValue<string>("Configuration:FilesPath"));

            if (!filesDirectory.Exists)
            {
                filesDirectory.Create();
            }

            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                options.UseSqlite($"Data Source={Path.Combine(filesDirectory.FullName, "Graylog2Grafana.db")}");
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
            // Scoped
            .AddScoped<IMonitorSeriesService, MonitorSeriesService>()
            .AddScoped<IMonitorSeriesDataService, MonitorSeriesDataService>()
            .AddScoped<IAnomalyDetectionDataService, AnomalyDetectionDataService>()
            .AddScoped<IGrafanaSimpleJsonPluginService, GrafanaSimpleJsonPluginService>()
            // Hosted services
            .AddHostedService<LoadDataWorker>()
            .AddHostedService<AnomalyDetectionWorker>();

            services.AddHttpClient("Graylog", c =>
            {
                var graylogConfiguration = Configuration.GetSection("Graylog").Get<GraylogConfiguration>();
                string base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{graylogConfiguration.Username}:{graylogConfiguration.Password}"));
                c.BaseAddress = new Uri(graylogConfiguration.Url);
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));              
            });

            services.AddHttpClient("Slack", c =>
            {
                var slackConfiguration = Configuration.GetSection("Slack").Get<SlackConfiguration>();
                c.BaseAddress = new Uri("https://slack.com");
                c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", slackConfiguration.BearerToken);
            });
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

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
