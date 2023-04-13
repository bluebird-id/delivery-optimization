namespace RouteOptimization.Utils.Helper
{
    public class ShipmentTime
    {
        public static async Task<long[,]> TimeWindows(List<Models.ShipmentModel> shipments, DateTime min, DateTime max)
        {
            long[,] timeWindows = new long[shipments.Count * 2 + 1, 2];
            timeWindows[0, 0] = min.Hour * 60;
            timeWindows[0, 1] = max.Hour * 60;
            for (int x = 1; x < shipments.Count + 1; x++)
            {
                timeWindows[x * 2 - 1, 0] = 0;
                timeWindows[x * 2 - 1, 1] = shipments[x - 1].Constraints.PickupTimeWindows.EndTime.ToMinute();
                timeWindows[x * 2, 0] = 0;
                timeWindows[x * 2, 1] = shipments[x - 1].Constraints.DropoffTimeWindows.EndTime.ToMinute();
            }
            return timeWindows;
        }
    }
}
