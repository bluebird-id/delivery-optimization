namespace RouteOptimization.Helpers
{
    public static class DateTimeHelper
    {
        public static long ToMinute(this DateTime dateTime) { 
            return dateTime.Hour * 60 + dateTime.Minute;
        }
    }
}
