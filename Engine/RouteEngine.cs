using RouteOptimization.Helpers;
using RouteOptimization.Models;
using RouteOptimization.Protos;
using RouteOptimization.Repositories.Client;
using RouteOptimization.Utils.Constans;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;
using Google.Type;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using Osrm.HttpApiClient;

namespace RouteOptimization.Engine
{
    public class RouteEngine
    {
        public async Task<List<Models.RouteModel>> RouteSolution(List<Models.ShipmentModel> lShipment, List<Models.VehicleModel> lVehicles, IConfiguration configuration)
        {
            List<Models.LocationModel> routes = new List<Models.LocationModel>();
            long index = 0;

            //shipment demands
            List<ShipmentDemand> lShipmetDemand = new List<ShipmentDemand>();
            for (int i = 0; i < lShipment[0].Constraints.Demands.Count; i++)
            {
                ShipmentDemand sShipmetDemand = new ShipmentDemand();
                long[] demand = new long[lShipment.Count * 2 + 1];
                int DemandType = lShipment[0].Constraints.Demands[i].Type;
                index = 0;
                foreach (var item in lShipment)
                {
                    foreach (var itemDemand in item.Constraints.Demands)
                    {
                        if (itemDemand.Type == DemandType)
                        {
                            demand[((index + 1) * 2) - 1] = (long)itemDemand.Demands;
                            demand[((index + 1) * 2)] = 0;
                        }
                    }
                    index++;
                }

                sShipmetDemand.Type = i;
                sShipmetDemand.Value = ((Types)i).ToString();
                sShipmetDemand.Demands = demand;
                lShipmetDemand.Add(sShipmetDemand);
            }

            //shipment location
            index = 0;
            foreach (var item in lShipment)
            {
                routes.Add(item.PickupLocation);
                routes.Add(item.DropoffLocation);

                index++;
            }

            //vehicle Capacity
            List<VehicleCapacity> lVehicleCapacity = new List<VehicleCapacity>();
            index = 0;
            for (int i = 0; i < lVehicles[0].Capacities.Count; i++)
            {
                VehicleCapacity sVehicleCapacity = new VehicleCapacity();
                long[] capacity = new long[lVehicles.Count];
                int CapacityType = lVehicles[0].Capacities[i].Type;
                index = 0;
                foreach (var vehicle in lVehicles)
                {
                    foreach (var itemCapacity in vehicle.Capacities)
                    {
                        if (itemCapacity.Type == CapacityType)
                        {
                            capacity[index] = (long)itemCapacity.Capacities;
                        }
                    }
                    index++;
                }

                sVehicleCapacity.Type = i;
                sVehicleCapacity.Value = ((Types)i).ToString();
                sVehicleCapacity.Capasities = capacity;
                lVehicleCapacity.Add(sVehicleCapacity);
            }

            //vehicle location
            routes.Add(lVehicles[0].StartLocation);

            //assign pickup and delivery point
            int node = 1;
            int[][] pickDelivery = new int[lShipment.Count][];
            for (int x = 0; x < lShipment.Count; x++)
            {
                pickDelivery[x] = new int[2];
                for (int y = 0; y < 2; y++)
                {
                    pickDelivery[x][y] = node;
                    node = node + 1;
                }
            }

            //time working
            List<System.DateTime> lTimeWorkingVh = new List<System.DateTime>();
            foreach (var item in lVehicles)
            {
                lTimeWorkingVh.Add(item.TimeWorking.StartTime);
                lTimeWorkingVh.Add(item.TimeWorking.EndTime);
            }
            System.DateTime minDate = lTimeWorkingVh.Cast<System.DateTime>().Min();
            System.DateTime maxDate = lTimeWorkingVh.Cast<System.DateTime>().Max();

            #region INIT DATAMODEL
            var oOsrm = new OsrmClient(configuration);
            (long[,] distanceMatrix, long[,] timeMatrix) = oOsrm.DataMatrix(routes).Result;

            Models.DataModel data = new Models.DataModel();
            data.DistanceMatrix = distanceMatrix;
            data.TimeMatrix = timeMatrix;
            data.TimeWindows = ShipmentTime.TimeWindows(lShipment, minDate, maxDate).Result;
            data.VehicleNumber = lVehicles.Count;
            data.PickupsDeliveries = pickDelivery;
            data.vehicleCapacity = lVehicleCapacity;
            data.shipmetDemand = lShipmetDemand;
            data.DepotCapacity = lVehicles.Count;
            //data.VehicleLoadTime = lVehicles[0].LoadTime;
            //data.VehicleUnloadTime = lVehicles[0].UnloadTime;
            data.Depot = 0;
            #endregion

            #region ROUTE ENGINE CUSTOMIZE
            RoutingIndexManager manager =
            new RoutingIndexManager(data.DistanceMatrix.GetLength(0), data.VehicleNumber, data.Depot);


            // Create Routing Model.
            RoutingModel routing = new RoutingModel(manager);

            // Create and register a transit callback.
            int transitCallbackIndexDistance = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                // Convert from routing variable Index to
                // distance matrix NodeIndex.
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                return data.DistanceMatrix[fromNode, toNode];
            });

            // Define cost of each arc.
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndexDistance);
            routing.AddDimension(transitCallbackIndexDistance, 0, Constant.MAX_VEHICLE_DISTANCE,
                                 true, // start cumul to zero
                                 "Distance");

            RoutingDimension distanceDimension = routing.GetMutableDimension("Distance");
            distanceDimension.SetGlobalSpanCostCoefficient(100);

            // Add demand constraint.
            for (int i = 0; i < data.vehicleCapacity.Count; i++)
            {
                int ItemIndex = i;
                if (data.vehicleCapacity[ItemIndex].Value != "")
                {
                    int demandCallbackIndex = routing.RegisterUnaryTransitCallback((long fromIndex) =>
                    {
                        // Convert from routing variable Index to
                        // demand NodeIndex.
                        var fromNode =
                        manager.IndexToNode(fromIndex);
                        return data.shipmetDemand[ItemIndex].Demands[fromNode];
                    });
                    routing.AddDimensionWithVehicleCapacity(demandCallbackIndex, 0,                     // null capacity slack
                                                            data.vehicleCapacity[ItemIndex].Capasities, // vehicle maximum capacities
                                                            true,                                       // start cumul to zero
                                                            data.vehicleCapacity[ItemIndex].Value);
                }
            }

            int transitCallbackIndexTime = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                // Convert from routing variable Index to
                // distance matrix NodeIndex.
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                return data.TimeMatrix[fromNode, toNode];
            });
            routing.AddDimension(transitCallbackIndexTime,  // transit callback
                                 1440,                      // allow waiting time
                                 1440,                      // vehicle maximum capacities
                                 false,                     // start cumul to zero
                                 "Time");

            RoutingDimension timeDimension = routing.GetMutableDimension("Time");
            timeDimension.SetGlobalSpanCostCoefficient(100);

            // Add time window constraints for each location except depot.
            for (int i = 1; i < data.TimeWindows.GetLength(0); ++i)
            {
                index = manager.NodeToIndex(i);
                timeDimension.CumulVar(index).SetRange(data.TimeWindows[i, 0], data.TimeWindows[i, 1]);
            }

            // Add time window constraints for each vehicle start node.
            for (int i = 0; i < data.VehicleNumber; ++i)
            {
                index = routing.Start(i);
                timeDimension.CumulVar(index).SetRange(data.TimeWindows[0, 0], data.TimeWindows[0, 1]);
            }

            // Define Transportation Requests.
            Solver solver = routing.solver();
            IntervalVar[] intervals = new IntervalVar[data.VehicleNumber * 2];
            for (int i = 0; i < data.VehicleNumber; ++i)
            {
                // Add load duration at start of routes
                intervals[2 * i] = solver.MakeFixedDurationIntervalVar(timeDimension.CumulVar(routing.Start(i)),
                                                                       lVehicles[i].LoadTime, "depot_interval");
                // Add unload duration at end of routes.
                intervals[2 * i + 1] = solver.MakeFixedDurationIntervalVar(timeDimension.CumulVar(routing.End(i)),
                                                                           lVehicles[i].UnloadTime, "depot_interval");
            }

            long[] depot_usage = Enumerable.Repeat<long>(1, intervals.Length).ToArray();
            solver.Add(solver.MakeCumulative(intervals, depot_usage, data.DepotCapacity, "depot"));

            // Instantiate route start and end times to produce feasible times.
            for (int i = 0; i < data.VehicleNumber; ++i)
            {
                routing.AddVariableMinimizedByFinalizer(timeDimension.CumulVar(routing.Start(i)));
                routing.AddVariableMinimizedByFinalizer(timeDimension.CumulVar(routing.End(i)));
            }

            for (int i = 0; i < data.PickupsDeliveries.GetLength(0); i++)
            {
                long pickupIndex = manager.NodeToIndex(data.PickupsDeliveries[i][0]);
                long deliveryIndex = manager.NodeToIndex(data.PickupsDeliveries[i][1]);
                routing.AddPickupAndDelivery(pickupIndex, deliveryIndex);
                solver.Add(solver.MakeEquality(routing.VehicleVar(pickupIndex), routing.VehicleVar(deliveryIndex)));
                solver.Add(solver.MakeLessOrEqual(distanceDimension.CumulVar(pickupIndex),
                                                  distanceDimension.CumulVar(deliveryIndex)));
            }

            // Setting first solution heuristic.
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
            searchParameters.TimeLimit = new Duration { Seconds = 10 };

            // Solve the problem.
            Assignment solution = routing.SolveWithParameters(searchParameters);
            PrintSolution(data, routing, manager, solution);

            #endregion

            #region Generate Response

            List<Models.RouteModel> lRoute = new List<Models.RouteModel>();
            int IndexVehicle = 0;

            foreach (Models.VehicleModel sVehicle in lVehicles)
            {
                List<Models.VisitModel> lVisit = new List<Models.VisitModel>();
                double[] Total = new double[lShipment[0].Constraints.Demands.Count];

                var IndexNode = routing.Start(IndexVehicle);
                while (routing.IsEnd(IndexNode) == false)
                {
                    int x = manager.IndexToNode(IndexNode);
                    var timeVar = timeDimension.CumulVar(x);

                    Int32 VisitType = 0;
                    if (x > 0)
                    {
                        if (x % 2 == 0)
                        {
                            VisitType = 2;
                        }
                        else
                        {
                            VisitType = 1;
                        }
                    }

                    Models.LocationModel sLocation = new Models.LocationModel();
                    Models.VisitModel sVisit = new Models.VisitModel();

                    sVisit.Type = VisitType;

                    List<Models.DemandModel> lResponseDemand = new List<Models.DemandModel>();

                    int indexShipment = 0;
                    if (x > 0)
                    {
                        if (x % 2 == 0)
                        {
                            indexShipment = (x / 2) - 1;
                            sVisit.Shipment = lShipment[indexShipment];
                        }
                        else
                        {
                            indexShipment = ((x + 1) / 2) - 1;
                            sVisit.Shipment = lShipment[indexShipment];
                        }

                        for (int i = 1; i <= lShipment[0].Constraints.Demands.Count; i++)
                        {
                            Models.DemandModel sResponseDemand = new Models.DemandModel();
                            foreach (var itemDemand in lShipment[indexShipment].Constraints.Demands)
                            {
                                if (itemDemand.Type == i)
                                {
                                    if (sVisit.Type == 1)
                                    {

                                        Total[i - 1] = Total[i - 1] + data.shipmetDemand[i - 1].Demands[x];
                                    }
                                    else
                                    {
                                        Total[i - 1] = Total[i - 1] - data.shipmetDemand[i - 1].Demands[x - 1];
                                    }
                                }
                            }

                            sResponseDemand.Type = i;
                            sResponseDemand.Demands = Total[i - 1];
                            lResponseDemand.Add(sResponseDemand);
                        }
                    }

                    long TimeMinute = solution.Min(timeVar);
                    sVisit.Eta = TimeSpan.FromMinutes(TimeMinute);
                    sVisit.Demand = lResponseDemand;

                    if (sVisit.Type != 0)
                    {
                        lVisit.Add(sVisit);
                    }

                    IndexNode = solution.Value(routing.NextVar(IndexNode));
                }

                Models.RouteModel sRoute = new Models.RouteModel();
                sRoute.Vehicle = lVehicles[IndexVehicle];
                sRoute.Visits = lVisit;

                if (sRoute.Visits.Count > 0)
                    lRoute.Add(sRoute);

                IndexVehicle++;
            }

            #endregion

            #region Generate WayPoints

            List<Models.WayPointModel> lWayPoint = new List<Models.WayPointModel>();
            int IndexVehicleWayPoint = 0;

            foreach (Models.VehicleModel sVehicle in lVehicles)
            {
                List<Models.LocationModel> lLocation = new List<Models.LocationModel>();

                var IndexNode = routing.Start(IndexVehicleWayPoint);
                while (routing.IsEnd(IndexNode) == false)
                {
                    int x = manager.IndexToNode(IndexNode);

                    Models.LocationModel sLocation = new Models.LocationModel();

                    if (x > 0)
                    {
                        if (x % 2 == 0)
                        {
                            sLocation = lShipment[(x / 2) - 1].DropoffLocation;
                        }
                        else
                        {
                            sLocation = lShipment[((x + 1) / 2) - 1].PickupLocation;
                        }
                    }
                    else
                    {
                        sLocation = lVehicles[IndexVehicleWayPoint].StartLocation;
                    }

                    IndexNode = solution.Value(routing.NextVar(IndexNode));

                    lLocation.Add(sLocation);
                }

                lLocation.Add(lVehicles[IndexVehicleWayPoint].EndLocation);

                if (lLocation.Count > 2)
                {
                    Models.WayPointModel sWayPoint = new Models.WayPointModel();
                    sWayPoint.WayPoints = lLocation;
                    lWayPoint.Add(sWayPoint);
                }

                IndexVehicleWayPoint++;
            }

            string json = JsonConvert.SerializeObject(lWayPoint);
            Console.WriteLine(json);

            #endregion

            return lRoute;
        }

        static void PrintSolution(in DataModel data, in RoutingModel routing, in RoutingIndexManager manager,
                                  in Assignment solution)
        {
            try
            {
                Console.WriteLine($"Objective {solution.ObjectiveValue()}:");

                // Inspect solution.
                RoutingDimension timeDimension = routing.GetMutableDimension("Time");
                long totalTime = 0;
                long totalDistance = 0;
                for (int i = 0; i < data.VehicleNumber; ++i)
                {
                    Console.WriteLine("Route for Vehicle {0}:", i);
                    long routeDistance = 0;
                    var index = routing.Start(i);
                    while (routing.IsEnd(index) == false)
                    {
                        var timeVar = timeDimension.CumulVar(index);
                        Console.Write("{0} Time({1},{2}) -> ", manager.IndexToNode(index), solution.Min(timeVar),
                                      solution.Max(timeVar));
                        var previousIndex = index;
                        index = solution.Value(routing.NextVar(index));
                        routeDistance += routing.GetArcCostForVehicle(previousIndex, index, 0);
                    }
                    var endTimeVar = timeDimension.CumulVar(index);
                    Console.WriteLine("{0} Time({1},{2})", manager.IndexToNode(index), solution.Min(endTimeVar),
                                      solution.Max(endTimeVar));
                    Console.WriteLine("Time of the route: {0}min", solution.Min(endTimeVar));
                    Console.WriteLine("Distance of the route: {0}m", routeDistance);
                    totalDistance += routeDistance;
                    totalTime += solution.Min(endTimeVar);
                }
                Console.WriteLine("Total Distance of all routes: {0}m", totalDistance);
                Console.WriteLine("Total time of all routes: {0}min", totalTime);
            }
            catch (Exception)
            {
                Console.WriteLine("No Solution");
            }
        }
    }
}
