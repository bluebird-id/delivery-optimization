using RouteOptimization.Models;
using RouteOptimization.Protos;
using RouteOptimization.Repositories;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Google.Type;
using Grpc.Core;
using System.Globalization;
using System;
using Google.Protobuf;
using RouteOptimization.Engine;
using Google.Apis.Util;
using DateTime = System.DateTime;
using Microsoft.OpenApi.Validations.Rules;
using RouteOptimization.Repositories.Client;

namespace RouteOptimization.Services
{
    public class RoutingSolverService : Protos.RouteOptimationService.RouteOptimationServiceBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RoutingSolverService> _log;
        private readonly IRepository _repo;

        public RoutingSolverService(IRepository repo, IConfiguration configuration, ILogger<RoutingSolverService> log)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public override async Task<resRoutesLite> BestRouteLite(reqModel request, ServerCallContext context)
        {
            try
            {
                double pickLatt = 0;
                double pickLong = 0;
                double dropLatt = 0;
                double dropLong = 0;
                System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var oOsrm = new OsrmClient(_configuration);

                Console.WriteLine("Begin call BestRoute service");
                List<Models.ShipmentModel> lShipment = new List<Models.ShipmentModel>();
                foreach (var item in request.Shipments)
                {
                    if (item.PickupLocation == null)
                    {
                        (pickLatt, pickLong) = oOsrm.GetGeoCode(item.PickupAddress).Result;
                    }

                    if (item.DropoffLocation == null)
                    {
                        (dropLatt, dropLong) = oOsrm.GetGeoCode(item.DropffAddress).Result;
                    }

                    var oPickup = new Models.LocationModel
                    {
                        Latitude = item.PickupLocation.Latitude,
                        Longitude = item.PickupLocation.Longitude,
                    };

                    var oDrops = new Models.LocationModel
                    {
                        Latitude = item.DropoffLocation.Latitude,
                        Longitude = item.DropoffLocation.Longitude,
                    };

                    List<Models.DemandModel> lDemand = new List<Models.DemandModel>();
                    foreach (var itemDemand in item.Constraints.Demand)
                    {
                        Models.DemandModel sDemand = new Models.DemandModel();
                        sDemand.Type = itemDemand.Type;
                        sDemand.Demands = itemDemand.Demand_;

                        lDemand.Add(sDemand);
                    }

                    lShipment.Add(new Models.ShipmentModel
                    {
                        ShipmentId = item.ShipmentId,
                        PickupAddress = item.PickupAddress,
                        PickupLocation = oPickup,
                        DropffAddress = item.DropffAddress,
                        DropoffLocation = oDrops,
                        Info = item.Info,

                        Constraints = new Models.ConstraintsModel
                        {
                            PickupTimeWindows = new Models.TimewindowsModel
                            {
                                StartTime = item.Constraints.PickupTimeWindows.StartTime.ToDateTime(),
                                EndTime = item.Constraints.PickupTimeWindows.EndTime.ToDateTime(),
                            },
                            DropoffTimeWindows = new Models.TimewindowsModel
                            {
                                StartTime = item.Constraints.DropoffTimeWindows.StartTime.ToDateTime(),
                                EndTime = item.Constraints.DropoffTimeWindows.EndTime.ToDateTime(),
                            },
                            Demands = lDemand,
                        }
                    });
                }

                List<Models.VehicleModel> lVehicle = new List<Models.VehicleModel>();
                foreach (var item in request.Vehicles)
                {
                    List<Models.CapacityModel> lCapacity = new List<Models.CapacityModel>();
                    foreach (var itemCapacity in item.Capacities)
                    {
                        Models.CapacityModel sCapacity = new Models.CapacityModel();
                        sCapacity.Type = itemCapacity.Type;
                        sCapacity.Capacities = itemCapacity.Capacity_;

                        lCapacity.Add(sCapacity);
                    }

                    lVehicle.Add(new Models.VehicleModel
                    {
                        VehicleId = item.VehicleId,
                        Capacities = lCapacity,
                        StartLocation = new Models.LocationModel { Latitude = item.StartLocation.Latitude, Longitude = item.StartLocation.Longitude },
                        EndLocation = new Models.LocationModel { Latitude = item.EndLocation.Latitude, Longitude = item.EndLocation.Longitude },
                        LoadTime = item.LoadTime,
                        UnloadTime = item.UnloadTime,
                        TimeWorking = new Models.TimewindowsModel
                        {
                            StartTime = item.TimeWorking.StartTime.ToDateTime(),
                            EndTime = item.TimeWorking.EndTime.ToDateTime()
                        },
                    });
                }

                //PROCESS ROUTE OPTIMAZION
                var oEng = new RouteEngine();
                var lRoute = await oEng.RouteSolution(lShipment, lVehicle, _configuration);

                resRoutesLite ret = new resRoutesLite();
                foreach (var r in lRoute)
                {
                    Protos.RouteLite oRoute = new Protos.RouteLite
                    {
                        VehicleId = r.Vehicle.VehicleId,
                    };
                    foreach (var v in r.Visits)
                    {
                        string TS = "";

                        if (v.Type == 1)
                        {
                            TS = v.Shipment.Constraints.PickupTimeWindows.StartTime.ToString("yyyy-MM-dd") + " " + v.Eta.ToString();
                        }
                        else
                        {
                            TS = v.Shipment.Constraints.DropoffTimeWindows.StartTime.ToString("yyyy-MM-dd") + " " + v.Eta.ToString();
                        }

                        List<Protos.Demand> lsDemand = new List<Protos.Demand>();
                        foreach (var d in v.Demand)
                        {
                            Protos.Demand oDemand = new Protos.Demand();

                            oDemand.Type = d.Type;
                            oDemand.Demand_ = d.Demands;

                            lsDemand.Add(oDemand);
                        }

                        var oVisit = new Protos.VisitLite
                        {
                            Eta = Timestamp.FromDateTime(DateTime.ParseExact(TS, "yyyy-MM-dd HH:mm:ss", null).ToUniversalTime()),
                            Type = v.Type,
                            ShipmentId = v.Shipment.ShipmentId
                        };
                        oVisit.Demand.Add(lsDemand);
                        oRoute.Visits.Add(oVisit);
                    }
                    ret.Routes.Add(oRoute);
                }

                return ret;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exception {ex.Message}");
                return new resRoutesLite();
            }
        }

        public override async Task<resRoutes> BestRoute(reqModel request, ServerCallContext context)
        {
            try
            {
                double pickLatt = 0;
                double pickLong = 0;
                double dropLatt = 0;
                double dropLong = 0;
                System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var oOsrm = new OsrmClient(_configuration);

                Console.WriteLine("Begin call BestRoute service");
                List<Models.ShipmentModel> lShipment = new List<Models.ShipmentModel>();
                foreach (var item in request.Shipments)
                {
                    if (item.PickupLocation == null)
                    {
                        (pickLatt, pickLong) = oOsrm.GetGeoCode(item.PickupAddress).Result;
                    }

                    if (item.DropoffLocation == null)
                    {
                        (dropLatt, dropLong) = oOsrm.GetGeoCode(item.DropffAddress).Result;
                    }

                    var oPickup = new Models.LocationModel
                    {
                        Latitude = item.PickupLocation.Latitude,
                        Longitude = item.PickupLocation.Longitude,
                    };

                    var oDrops = new Models.LocationModel
                    {
                        Latitude = item.DropoffLocation.Latitude,
                        Longitude = item.DropoffLocation.Longitude,
                    };

                    List<Models.DemandModel> lDemand = new List<Models.DemandModel>();
                    foreach (var itemDemand in item.Constraints.Demand)
                    {
                        Models.DemandModel sDemand = new Models.DemandModel();
                        sDemand.Type = itemDemand.Type;
                        sDemand.Demands = itemDemand.Demand_;

                        lDemand.Add(sDemand);
                    }

                    lShipment.Add(new Models.ShipmentModel
                    {

                        ShipmentId = item.ShipmentId,
                        PickupAddress = item.PickupAddress,
                        PickupLocation = oPickup,
                        DropffAddress = item.DropffAddress,
                        DropoffLocation = oDrops,
                        Info = item.Info,

                        Constraints = new Models.ConstraintsModel
                        {
                            PickupTimeWindows = new Models.TimewindowsModel
                            {
                                StartTime = item.Constraints.PickupTimeWindows.StartTime.ToDateTime(),
                                EndTime = item.Constraints.PickupTimeWindows.EndTime.ToDateTime(),
                            },
                            DropoffTimeWindows = new Models.TimewindowsModel
                            {
                                StartTime = item.Constraints.DropoffTimeWindows.StartTime.ToDateTime(),
                                EndTime = item.Constraints.DropoffTimeWindows.EndTime.ToDateTime(),
                            },
                            Demands = lDemand,
                        }
                    });
                }

                List<Models.VehicleModel> lVehicle = new List<Models.VehicleModel>();
                foreach (var item in request.Vehicles)
                {
                    List<Models.CapacityModel> lCapacity = new List<Models.CapacityModel>();
                    foreach (var itemCapacity in item.Capacities)
                    {
                        Models.CapacityModel sCapacity = new Models.CapacityModel();
                        sCapacity.Type = itemCapacity.Type;
                        sCapacity.Capacities = itemCapacity.Capacity_;

                        lCapacity.Add(sCapacity);
                    }

                    lVehicle.Add(new Models.VehicleModel
                    {
                        VehicleId = item.VehicleId,
                        Capacities = lCapacity,
                        StartLocation = new Models.LocationModel { Latitude = item.StartLocation.Latitude, Longitude = item.StartLocation.Longitude },
                        EndLocation = new Models.LocationModel { Latitude = item.EndLocation.Latitude, Longitude = item.EndLocation.Longitude },
                        LoadTime = item.LoadTime,
                        UnloadTime = item.UnloadTime,
                        TimeWorking = new Models.TimewindowsModel
                        {
                            StartTime = item.TimeWorking.StartTime.ToDateTime(),
                            EndTime = item.TimeWorking.EndTime.ToDateTime()
                        },
                    });
                }

                //PROCESS ROUTE OPTIMAZION
                var oEng = new RouteEngine();
                var lRoute = await oEng.RouteSolution(lShipment, lVehicle, _configuration);

                resRoutes ret = new resRoutes();
                foreach (var r in lRoute)
                {
                    List<Protos.Capacity> lsCapacity = new List<Protos.Capacity>();
                    foreach (var c in r.Vehicle.Capacities)
                    {
                        Protos.Capacity oCapacity = new Protos.Capacity();

                        oCapacity.Type = c.Type;
                        oCapacity.Capacity_ = c.Capacities;

                        lsCapacity.Add(oCapacity);
                    }

                    Protos.Route oRoute = new Protos.Route
                    {
                        Vehicle = new Protos.Vehicle
                        {
                            VehicleId = r.Vehicle.VehicleId,
                            StartLocation = new Protos.Location
                            {
                                Latitude = r.Vehicle.StartLocation.Latitude,
                                Longitude = r.Vehicle.StartLocation.Longitude
                            },
                            EndLocation = new Protos.Location
                            {
                                Longitude = r.Vehicle.EndLocation.Longitude,
                                Latitude = r.Vehicle.EndLocation.Latitude,
                            },
                            LoadTime = r.Vehicle.LoadTime,
                            UnloadTime = r.Vehicle.UnloadTime,
                            TimeWorking = new Protos.Timewindows
                            {
                                StartTime = r.Vehicle.TimeWorking.StartTime.ToTimestamp(),
                                EndTime = r.Vehicle.TimeWorking.EndTime.ToTimestamp(),
                            }
                        },
                    };

                    oRoute.Vehicle.Capacities.Add(lsCapacity);

                    foreach (var v in r.Visits)
                    {
                        string TS = "";

                        if (v.Type == 1)
                        {
                            TS = v.Shipment.Constraints.PickupTimeWindows.StartTime.ToString("yyyy-MM-dd") + " " + v.Eta.ToString();
                        }
                        else
                        {
                            TS = v.Shipment.Constraints.DropoffTimeWindows.StartTime.ToString("yyyy-MM-dd") + " " + v.Eta.ToString();
                        }

                        List<Protos.Demand> lsDemand = new List<Protos.Demand>();
                        foreach (var d in v.Demand)
                        {
                            Protos.Demand oDemand = new Protos.Demand();

                            oDemand.Type = d.Type;
                            oDemand.Demand_ = d.Demands;

                            lsDemand.Add(oDemand);
                        }

                        List<Protos.Demand> lsSipmentDemand = new List<Protos.Demand>();
                        foreach (var d in v.Shipment.Constraints.Demands)
                        {
                            Protos.Demand oDemand = new Protos.Demand();

                            oDemand.Type = d.Type;
                            oDemand.Demand_ = d.Demands;

                            lsSipmentDemand.Add(oDemand);
                        }

                        var oVisit = new Protos.Visit
                        {
                            Eta = Timestamp.FromDateTime(DateTime.ParseExact(TS, "yyyy-MM-dd HH:mm:ss", null).ToUniversalTime()),
                            Type = v.Type,
                            Shipment = new Protos.Shipment
                            {
                                Info = v.Shipment.Info,
                                ShipmentId = v.Shipment.ShipmentId,
                                PickupAddress = v.Shipment.PickupAddress,
                                DropffAddress = v.Shipment.DropffAddress,
                                PickupLocation = new Protos.Location
                                {
                                    Latitude = v.Shipment.PickupLocation.Latitude,
                                    Longitude = v.Shipment.PickupLocation.Longitude
                                },
                                DropoffLocation = new Protos.Location
                                {
                                    Latitude = v.Shipment.DropoffLocation.Latitude,
                                    Longitude = v.Shipment.DropoffLocation.Longitude
                                },
                                Constraints = new Protos.Constraints
                                {
                                    PickupTimeWindows = new Protos.Timewindows
                                    {
                                        StartTime = Timestamp.FromDateTime(v.Shipment.Constraints.PickupTimeWindows.StartTime),
                                        EndTime = Timestamp.FromDateTime(v.Shipment.Constraints.PickupTimeWindows.EndTime),
                                    },
                                    DropoffTimeWindows = new Protos.Timewindows
                                    {
                                        StartTime = Timestamp.FromDateTime(v.Shipment.Constraints.DropoffTimeWindows.StartTime),
                                        EndTime = Timestamp.FromDateTime(v.Shipment.Constraints.DropoffTimeWindows.EndTime),
                                    }
                                },
                            }
                        };
                        oVisit.Shipment.Constraints.Demand.Add(lsSipmentDemand);
                        oVisit.Demand.Add(lsDemand);
                        oRoute.Visits.Add(oVisit);
                    }
                    ret.Routes.Add(oRoute);
                }

                return ret;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exception {ex.Message}");
                return new resRoutes();
            }
        }
    }
}
