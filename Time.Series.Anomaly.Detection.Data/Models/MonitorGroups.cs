using System.ComponentModel.DataAnnotations;

namespace Time.Series.Anomaly.Detection.Data.Models
{
    public class MonitorGroups
    {
        public int ID { get; set; }

        [Required]
        public string Name { get; set; }
    }
}