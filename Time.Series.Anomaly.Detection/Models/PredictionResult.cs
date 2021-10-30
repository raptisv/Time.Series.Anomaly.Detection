namespace Time.Series.Anomaly.Detection.Models
{
    public struct PredictionResult
    {
        public int Index;
        public double Data;
        public string TimeStamp;
        public double Upper;
        public double Lower;
        public bool IsAnomaly;

        public PredictionResult(int index, double data, string timeStamp, double upper, double lower, bool isAnomaly)
        {
            this.Index = index;
            this.Data = data;
            this.TimeStamp = timeStamp;
            this.Upper = upper;
            this.Lower = lower;
            this.IsAnomaly = isAnomaly;
        }
    }
}
