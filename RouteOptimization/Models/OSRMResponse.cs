using System.Collections.Generic;

namespace RouteOptimization.Models
{
    public class OSRMResponse
    {
        public string? Code { get; set; }
        public List<List<double>> Distances { get; set; }
        public List<List<double>> Durations { get; set; }
        public List<LocationModel> Drops { get; set; }
    }
}
