namespace RouteOptimization.Utils.Helper
{
    public static class DateTimeHelper
    {
        public static long ToMinute(this DateTime dateTime)
        {
            return dateTime.Hour * 60 + dateTime.Minute;
        }
    }
}
