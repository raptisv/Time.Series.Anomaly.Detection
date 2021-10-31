using Microsoft.EntityFrameworkCore;

namespace Time.Series.Anomaly.Detection.Data.Models
{
    public class ApplicationDbContext : DbContext
    {
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

            builder.Entity<MonitorSeries>().ToTable("MonitorSeries").HasKey(p => p.ID);
            builder.Entity<MonitorSeries>().HasIndex(p => p.Name).IsUnique();

            builder.Entity<MonitorSeriesData>().ToTable("MonitorSeriesData").HasKey(p => p.ID);
            builder.Entity<MonitorSeriesData>().HasOne(p => p.MonitorSeries).WithMany(b => b.MonitorSeriesData).HasForeignKey(p => p.MonitorSeriesID).IsRequired();
            builder.Entity<MonitorSeriesData>().HasIndex(p => new { p.MonitorSeriesID, p.Timestamp }).IsUnique();

            builder.Entity<AnomalyDetectionData>().ToTable("AnomalyDetectionData").HasKey(p => p.ID);
            builder.Entity<AnomalyDetectionData>().HasOne(p => p.MonitorSeries).WithMany(b => b.AnomalyDetectionData).HasForeignKey(p => p.MonitorSeriesID).IsRequired();
            builder.Entity<AnomalyDetectionData>().HasIndex(p => new { p.MonitorSeriesID, p.Timestamp }).IsUnique();

            // Seed 
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
                    DoNotAlertAgainWithinMinutes = null
                });
        }
    }
}
