using System.ComponentModel.DataAnnotations;

namespace Time.Series.Anomaly.Detection.Data.Models.Enums
{
    public enum MonitorType
    {
        [Display(Name = "Downwards and upwards spikes")]
        DownwardsAndUpwards,

        [Display(Name = "Downwards spikes")]
        Downwards,

        [Display(Name = "Upwards spikes")]
        Upwards
    }
}
