using System.Net;

namespace ElasticMatch
{
    public interface IGameServer
    {
        public EndPoint Remote { get; set; }
        public EndPoint Address { get; set; }
    }
}