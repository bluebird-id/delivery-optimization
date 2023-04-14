using RouteOptimization.Models;

namespace RouteOptimization.Engine
{
    public partial class RouteEngine
    {
        /// </summary>
        /// Get list of shipment demand.
        /// </summary>
        internal List<Models.ShipmentDemand> GetShipmentDemands( List<Models.ShipmentModel> shipments )
        {
            var shipmentDemandList = new List<ShipmentDemand>();
            var demands = shipments.First().Constraints.Demands;
            var numDemands = demands.Count;

            for( int i = 0; i < numDemands; i++ )
            {
                long[] demandArray = new long[ shipments.Count * 2 + 1 ];
                int DemandType = demands[ i ].Type;
                int index = 0;

                foreach( var s in shipments )
                {
                    foreach( var d in s.Constraints.Demands )
                    {
                        if( d.Type == DemandType )
                        {
                            var j = ( index + 1 ) * 2;
                            demandArray[ j - 1 ] = (long) d.Demands;
                            demandArray[ j ] = 0;
                        }
                    }
                    index++;
                }

                shipmentDemandList.Add(
              new ShipmentDemand()
              {
                  Type = i,
                  Value = ( (EType) i ).ToString(),
                  Demands = demandArray
              } );
            }
            return shipmentDemandList;
        }
        /// </summary>
        /// description
        /// </summary>
        private void AppendLocationToRoutes( List<Models.LocationModel> routes, List<Models.ShipmentModel> shipments )
        {
            foreach( var s in shipments )
            {
                routes.Add( s.PickupLocation );
                routes.Add( s.DropoffLocation );
            }
        }

        /// </summary>
        /// Get vehicles capacites.
        /// </summary>
        internal List<Models.VehicleCapacity> GetVehicleCapacities( List<Models.VehicleModel> vehicles )
        {
            var result = new List<VehicleCapacity>();

            var index = 0;
            for( int i = 0; i < vehicles[ 0 ].Capacities.Count; i++ )
            {
                var sVehicleCapacity = new VehicleCapacity();
                long[] vCapacity = new long[ vehicles.Count ];
                int CapacityType = vehicles[ 0 ].Capacities[ i ].Type;
                index = 0;
                foreach( var v in vehicles )
                {
                    foreach( var c in v.Capacities )
                    {
                        if( c.Type == CapacityType )
                        {
                            vCapacity[ index ] = (long) c.Capacities;
                        }
                    }
                    index++;
                }

                sVehicleCapacity.Type = i;
                sVehicleCapacity.Value = ( (EType) i ).ToString();
                sVehicleCapacity.Capasities = vCapacity;
                result.Add( sVehicleCapacity );
            }
            return result;
        }

        /// </summary>
        /// Get pick up delivery points.
        /// </summary>
        internal int[][] GetPickUpPoints( List<Models.ShipmentModel> shipments )
        {
            var pickDelivery = new int[ shipments.Count ][];
            int node = 1;

            for( int i = 0; i < shipments.Count; i++ )
            {
                pickDelivery[ i ] = new int[ 2 ];
                pickDelivery[ i ][ 0 ] = node++;
                pickDelivery[ i ][ 1 ] = node++;
            }
            return pickDelivery;
        }

        private List<DateTime> GetVehicleWorkingTime( List<Models.VehicleModel> vehicleList )
        {
            var result = new List<DateTime>();

            foreach( var item in vehicleList )
            {
                result.Add( item.TimeWorking.StartTime );
                result.Add( item.TimeWorking.EndTime );
            }
            return result;
        }
    }
}