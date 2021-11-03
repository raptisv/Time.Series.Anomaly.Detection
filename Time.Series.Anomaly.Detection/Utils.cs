using System;
using System.Linq;

namespace Time.Series.Anomaly.Detection.Models
{
    public static class Utils
    {
        public static long GetUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date - origin;
            return (long)Math.Floor(diff.TotalSeconds);
        }

        public static long GetUnixTimestampMilliseconds(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date - origin;
            return (long)diff.TotalMilliseconds;
        }

        public static DateTime TruncateToMinute(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, 0);
        }

        public static DateTime? GetDateFromUnixTimestamp(string unixTime)
        {
            unixTime ??= string.Empty;

            if (unixTime.All(char.IsDigit))
            {
                if (long.TryParse(unixTime, out long number) && number >= 0)
                {
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    switch (unixTime.Length)
                    {
                        case 10:
                            return epoch.AddSeconds(number);
                        case 13:
                            return epoch.AddMilliseconds(number);
                    }
                }
            }

            return null;
        }

        public static string TimeAgo(this DateTime utcDateTime)
        {
            var timeSpan = DateTime.UtcNow.Subtract(utcDateTime);

            if (timeSpan <= TimeSpan.FromSeconds(60))
            {
                return  string.Format("{0} seconds ago", timeSpan.Seconds);
            }
            else if (timeSpan <= TimeSpan.FromMinutes(60))
            {
                return  timeSpan.Minutes > 1 ?
                    String.Format("{0} minutes ago", timeSpan.Minutes) :
                    "about a minute ago";
            }
            else if (timeSpan <= TimeSpan.FromHours(24))
            {
                return  timeSpan.Hours > 1 ?
                    String.Format("{0} hours ago", timeSpan.Hours) :
                    "about an hour ago";
            }
            else if (timeSpan <= TimeSpan.FromDays(30))
            {
                return  timeSpan.Days > 1 ?
                    String.Format("{0} days ago", timeSpan.Days) :
                    "yesterday";
            }
            else if (timeSpan <= TimeSpan.FromDays(365))
            {
                return  timeSpan.Days > 30 ?
                    String.Format("{0} months ago", timeSpan.Days / 30) :
                    "about a month ago";
            }
            else
            {
                return  timeSpan.Days > 365 ?
                    String.Format("{0} years ago", timeSpan.Days / 365) :
                    "about a year ago";
            }
        }
    }
}
