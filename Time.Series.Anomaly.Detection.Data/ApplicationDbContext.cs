using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Time.Series.Anomaly.Detection.Data.Models.Notifications;

namespace Time.Series.Anomaly.Detection.Data.Models
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public DbSet<NotificationSlack> SlackNotification { get; set; }
        public DbSet<MonitorSources> MonitorSources { get; set; }
        public DbSet<MonitorSeries> MonitorSeries { get; set; }
        public DbSet<MonitorSeriesData> MonitorSeriesData { get; set; }
        public DbSet<AnomalyDetectionData> AnomalyDetectionData { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Core Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Core Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            builder.Entity<MonitorSources>().ToTable("MonitorSources").HasKey(p => p.ID);
            builder.Entity<MonitorSources>().HasIndex(p => p.Name).IsUnique();

            builder.Entity<NotificationSlack>().ToTable("SlackNotification").HasKey(p => p.ID);

            builder.Entity<MonitorSeries>().ToTable("MonitorSeries").HasKey(p => p.ID);
            builder.Entity<MonitorSeries>().HasOne(p => p.MonitorSource).WithMany(b => b.MonitorSeriesData).HasForeignKey(p => p.MonitorSourceID).IsRequired();
            builder.Entity<MonitorSeries>().HasIndex(p => p.Name).IsUnique();

            builder.Entity<MonitorSeriesData>().ToTable("MonitorSeriesData").HasKey(p => p.ID);
            builder.Entity<MonitorSeriesData>().HasOne(p => p.MonitorSeries).WithMany(b => b.MonitorSeriesData).HasForeignKey(p => p.MonitorSeriesID).IsRequired();
            builder.Entity<MonitorSeriesData>().HasIndex(p => new { p.MonitorSeriesID, p.Timestamp }).IsUnique();

            builder.Entity<AnomalyDetectionData>().ToTable("AnomalyDetectionData").HasKey(p => p.ID);
            builder.Entity<AnomalyDetectionData>().HasOne(p => p.MonitorSeries).WithMany(b => b.AnomalyDetectionData).HasForeignKey(p => p.MonitorSeriesID).IsRequired();
            builder.Entity<AnomalyDetectionData>().HasIndex(p => new { p.MonitorSeriesID, p.Timestamp }).IsUnique();

            // Seed 
            var slackNotification = new NotificationSlack()
            {
                ID = 1,
                Enabled = false,
                Channel = string.Empty,
                BearerToken = string.Empty,
            };

            builder.Entity<NotificationSlack>().HasData(slackNotification);

            var firstMonitorSource = new MonitorSources()
            {
                ID = 1,
                Enabled = true,
                Name = "Local",
                SourceType = Enums.SourceType.Graylog,
                Source = "http://localhost:9000",
                Username = "admin",
                Password = "admin",
                DataRetentionInMinutes = 1440,
                DetectionDelayInMinutes = 2,
                LoadDataIntervalSeconds = 60
            };

            builder.Entity<MonitorSources>().HasData(firstMonitorSource);

            builder.Entity<MonitorSeries>().HasData(
                new MonitorSeries()
                {
                    ID = 1,
                    Name = "all_logs",
                    Query = "*",
                    Sensitivity = 90,
                    LowerLimitToDetect = null,
                    UpperLimitToDetect = null,
                    MonitorType = Enums.MonitorType.DownwardsAndUpwards,
                    Description = "Initial dummy query",
                    MinuteDurationForAnomalyDetection = 60,
                    DoNotAlertAgainWithinMinutes = null,
                    MonitorSourceID = firstMonitorSource.ID,
                    Aggregation = "count"
                });

            // Seed Admin role
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole() { Id = "fab4fac1-c546-41de-aebc-a14da6895711", Name = "admin", ConcurrencyStamp = "1", NormalizedName = "admin" });

            // Seed admin user
            string adminUsername = "admin";
            string adminPassword = "admin";
            IdentityUser adminUser = new IdentityUser()
            {
                Id = "b74ddd14-6340-4840-95c2-db12554843e5",
                UserName = adminUsername,
                Email = adminUsername,
                NormalizedEmail = adminUsername.ToUpper(),
                NormalizedUserName = adminUsername.ToUpper(),
                LockoutEnabled = false,
                TwoFactorEnabled = false,
                EmailConfirmed = true,
                SecurityStamp = "9e9d98d1-d7b6-4ac4-ae83-bd4819e10ecf"
            };
            PasswordHasher<IdentityUser> passwordHasher = new PasswordHasher<IdentityUser>();
            var initialAdminPassword = adminPassword;
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, initialAdminPassword);
            builder.Entity<IdentityUser>().HasData(adminUser);

            // Bind Admin to role
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>() { RoleId = "fab4fac1-c546-41de-aebc-a14da6895711", UserId = "b74ddd14-6340-4840-95c2-db12554843e5" });
        }
    }
}
