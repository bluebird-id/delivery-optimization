namespace RouteOptimization.Models
{
    public class ShipmentOrder
    {
        public List<ShipmentModel> shipments { get; set; }
        public List<VehicleModel> vehicles { get; set; }
    }
}
