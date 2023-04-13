using RouteOptimization.Repositories.Client;
namespace RouteOptimization.Repositories
{

    public interface IRepository
    {
        Client.OsrmClient Osrm();
        
    }

    public class ORToolsRepository : IRepository
    {
        private readonly Client.OsrmClient _osrm;

        public ORToolsRepository(Client.OsrmClient osrmClient) //, Client.ORToolsClient orClient)
        {
            _osrm = osrmClient ?? throw new ArgumentNullException(nameof(osrmClient));
        }


        public OsrmClient Osrm()
        {
            return _osrm;
        }
    }
}
