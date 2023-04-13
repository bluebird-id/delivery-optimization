using RouteOptimization.Helpers;
using RouteOptimization.Models;
using RouteOptimization.Protos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Osrm.HttpApiClient;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace RouteOptimization.Repositories.Client
{
    public class OsrmClient
    {
        private readonly IConfiguration _configuration;

        public OsrmClient(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<(long[,], long[,])> DataMatrix(List<Models.LocationModel> routes)
        {
            string OsrmUrl = _configuration["Osrm:Url"];
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(OsrmUrl),
            };

            var osrmClient = new OsrmHttpApiClient(httpClient);
            Coordinate[] coordinates = new Coordinate[routes.Count];
            int index = 0;
            foreach (var item in routes)
            {
                double latitude = item.Latitude;
                double longitude = item.Longitude;
                Coordinate coordinate = Coordinate.Create(longitude, latitude);
                coordinates.SetValue(coordinate, index++);
            }

            var tableRequest = OsrmServices
                .Table("driving", GeographicalCoordinates.Create(coordinates))
                .Annotations(TableAnnotations.DurationAndDistance)
                .Build();

            var response = await osrmClient.GetTableAsync(tableRequest);
            long[,] distanceMatrix = new long[response.Distances.Length, response.Distances.Length];

            int i = 0;
            foreach (var distance in response.Distances)
            {
                int j = 0;
                distance.ToList().ForEach(
                   d =>
                   {
                       distanceMatrix[i, j] = (d == null ? 0 : (long)d);
                       j++;
                   }
                );
                i++;
            }

            long[,] timeMatrix = new long[response.Distances.Length, response.Distances.Length];
            i = 0;
            foreach (var duration in response.Durations)
            {
                int j = 0;
                duration.ToList().ForEach(
                   d =>
                   {
                       timeMatrix[i, j] = (d == null ? 0 : (long)d) / 60;  // on minute
                       j++;
                   }
                );
                i++;
            }

            return (distanceMatrix, timeMatrix);
        }

        public async Task<(double, double)> GetGeoCode(string address)
        {
            var uriOsrm = _configuration["Osrm:Url"];
            Console.Write(uriOsrm);
            var client = new RestClient($"{uriOsrm}");
            var request = new RestRequest($"search/{address}", RestSharp.Method.Get);
            request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,/;q=0.8");
            request.AddHeader("Accept-Encoding", "gzip, deflate, br");
            request.AddOrUpdateParameter("format", "json");
            request.AddOrUpdateParameter("addressdetails", "1");
            request.AddOrUpdateParameter("limit", "1");
            var response = await client.ExecuteAsync(request);

            dynamic json = JsonConvert.DeserializeObject(response.Content);
            // get values lat, lon
            string lat = json[0].lat;
            string lon = json[0].lon;

            return (Convert.ToDouble(lat), Convert.ToDouble(lon));
        }

        public async Task<(double, double)> GetCoordinatesAsync(string address)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var uriNominatim = _configuration["Nominatim:Url"];

                string url = $"{uriNominatim}/search?q={address}&format=json&limit=1";
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    JArray results = JArray.Parse(responseString);

                    if (results.Count > 0)
                    {
                        JObject result = (JObject)results[0];
                        double latitude = (double)result["lat"];
                        double longitude = (double)result["lon"];
                        return (longitude, latitude);
                    }
                    else
                    {
                        throw new Exception("Address cannot be found");
                    }
                }
                else
                {
                    throw new Exception("Error when access API");
                }
            }
        }
    }
}
