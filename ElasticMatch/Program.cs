using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Ninject;
using NLog;

namespace ElasticMatch
{
    class Program
    {
        private static ILogger _logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            _logger.Info("Starting ElasticMatch..");

            var config =
                JsonConvert.DeserializeObject<MatchConfiguration>(File.ReadAllText(@"config.json"));
            var pref = new MatchPreferences();

            var kernel = Installer.Install(config, pref);
            {
                var serviceListener = kernel.Get<IServiceListener>();
                serviceListener.RunAsync().Wait();

                _logger.Info("ElasticMatch is successfully started.");
                while (true)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}