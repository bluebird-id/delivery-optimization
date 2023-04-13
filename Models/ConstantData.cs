using Newtonsoft.Json;

namespace RouteOptimization.Models
{
    public class ConstantData
    {
        public Int32 id { get; set; }
        public Int32 SeacrhTimeLimit { get; set; }
        public Int32 MaxVehicleDistance { get; set; }
        public Int32 MaxActiveVehicle { get; set; }
    }

    public class LocationModel
    {
        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }
    }


    public class TimewindowsModel
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class DemandModel
    {
        public int Type { get; set; }
        public double Demands { get; set; }
    }

    public class CapacityModel
    {
        public int Type { get; set; }
        public double Capacities { get; set; }
    }


    public class ConstraintsModel
    {
        public TimewindowsModel PickupTimeWindows { get; set; }
        public TimewindowsModel DropoffTimeWindows { get; set; }
        public List<DemandModel> Demands { get; set; }
    }

    public class ShipmentModel
    {
        public string ShipmentId { get; set; }
        public LocationModel PickupLocation { get; set; }
        public LocationModel DropoffLocation { get; set; }
        public string PickupAddress { get; set; }
        public string DropffAddress { get; set; }
        public ConstraintsModel Constraints { get; set; }
        public string Info { get; set; }
    }

    public class VehicleModel
    {
        public string VehicleId { get; set; }
        public List<CapacityModel> Capacities { get; set; }
        public LocationModel StartLocation { get; set; }
        public LocationModel EndLocation { get; set; }
        public int LoadTime { get; set; }  // satuan menit
        public int UnloadTime { get; set; }
        public TimewindowsModel TimeWorking { get; set; }
    }

    public class VisitModel
    {
        public int Type { get; set; }
        public ShipmentModel Shipment { get; set; }

        //estimate time arrival
        public TimeSpan Eta { get; set; }

        public List<DemandModel> Demand { get; set; }
    }

    public class RouteModel
    {
        public VehicleModel Vehicle { get; set; }
        public List<VisitModel> Visits { get; set; }
    }

    public class WayPointModel
    {
        [JsonProperty("waypoints")]
        public List<LocationModel> WayPoints { get; set; }
    }

}
