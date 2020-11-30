using ElasticMatch.Protocol;
using NLog;

namespace ElasticMatch
{
    public class ServiceListener : TcpServiceListener
    {
        private IGameServerCoordinator _gameServerCoordinator;
        private IMatchMaker _matchMaker;
        private ILogger _logger = LogManager.GetCurrentClassLogger();

        public ServiceListener(int port, int soBacklog, IGameServerCoordinator gameServerCoordinator,
            IMatchMaker matchMaker) : base(port, soBacklog)
        {
            _gameServerCoordinator = gameServerCoordinator;
            _matchMaker = matchMaker;

            BindMessage<Hello>(OnHelloMatchmaker);
        }

        private void OnHelloMatchmaker(IServiceListenerContext context, Hello hello)
        {
            _logger.Info(hello.Message);
            _matchMaker.Hello(hello.Message);
        }
    }
}