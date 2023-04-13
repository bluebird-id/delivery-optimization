namespace RouteOptimization.Utils.Helper
{
    public class ORTools
    {
        static (double, double) TravelTime(Models.LocationModel fromLocation, Models.LocationModel toLocation, int speed)
        {
            double distance = CalculateDistance(fromLocation.Latitude, fromLocation.Longitude, toLocation.Latitude, toLocation.Longitude);

            // Calculate the travel time in hours
            double travelTime = distance / speed;
            return (distance, travelTime);
        }

        /// <summary>
        /// Calculated distance between coordinate lon, lat
        /// usage : double distance = CalculateDistance(latitude1, longitude1, latitude2, longitude2);
        /// </summary>
        /// <param name="lat1">coordinate latt 1st position</param>
        /// <param name="lon1">coordinate long 1st position</param>
        /// <param name="lat2">coordinate latt 2nd position</param>
        /// <param name="lon2">coordinate long 2nd position</param>
        /// <returns>double</returns>
        static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double earthRadius = 6371.0; // Radius of the earth in km

            // Convert the coordinates to radians
            lat1 = ToRadians(lat1);
            lon1 = ToRadians(lon1);
            lat2 = ToRadians(lat2);
            lon2 = ToRadians(lon2);

            // Calculate the difference between the coordinates
            double latDiff = lat2 - lat1;
            double lonDiff = lon2 - lon1;

            // Apply the Haversine formula
            double a = Math.Sin(latDiff / 2) * Math.Sin(latDiff / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) *
                       Math.Sin(lonDiff / 2) * Math.Sin(lonDiff / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = earthRadius * c;

            return distance;
        }

        static double ToRadians(double deg)
        {
            return deg * (Math.PI / 180.0);
        }
    }
}
