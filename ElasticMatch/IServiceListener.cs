using System.Threading.Tasks;

namespace ElasticMatch
{
    public interface IServiceListener
    {
        Task RunAsync();

        Task StopAsync();
    }
}