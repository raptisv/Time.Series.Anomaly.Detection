namespace Graylog2Grafana.Models.Configuration
{
    public class SlackConfiguration
    {
        public bool Enabled { get; set; }
        public string Channel { get; set; }
        public string BearerToken { get; set; }
    }
}
