using System.ComponentModel.DataAnnotations;

namespace Time.Series.Anomaly.Detection.Data.Models.Notifications
{
    public class NotificationSlack
    {
        public int ID { get; set; }

        [Required]
        public bool Enabled { get; set; }

        [Required]
        public string Channel { get; set; }

        [Required]
        public string BearerToken { get; set; }
    }
}