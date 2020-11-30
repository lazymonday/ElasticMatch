using Ninject;
using NLog;

namespace ElasticMatch
{
    public class Installer
    {
        public static IKernel Install(MatchConfiguration config, MatchPreferences pref)
        {
            var kernel = new StandardKernel();

            kernel.Bind<ILogger>().ToMethod(_ =>
                LogManager.GetLogger(_.Request.Target != null
                    ? _.Request.Target.Member.DeclaringType.Name
                    : _.Request.ToString()));

            kernel.Bind<IServiceListener>()
                .To<ServiceListener>()
                .InSingletonScope()
                .WithConstructorArgument("port", config.Port)
                .WithConstructorArgument("soBacklog", config.SoBacklog);

            kernel.Bind<IGameServerCoordinator>()
                .To<GameServerCoordinator>()
                .InSingletonScope();

            kernel.Bind<IMatchMaker>()
                .To<MatchMaker>()
                .InSingletonScope();

            return kernel;
        }
    }
}