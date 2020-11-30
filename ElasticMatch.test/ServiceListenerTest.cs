using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ElasticMatch.Protocol;
using ElasticMatch.test.Mock;
using ElasticMatch.Util;
using Google.Protobuf;
using NUnit.Framework;

namespace ElasticMatch.test
{
    public class ServiceListenerTest
    {
        private int _port;
        private int _soBacklog;
        private TcpClient _client;

        private IGameServerCoordinator _gameServerCooridnator;
        private IMatchMaker _matchMaker = new MockMatchMaker();
        private DnsEndPoint _address;
        private ServiceListener _testee;

        [SetUp]
        public void Setup()
        {
            _port = FreeBindablePortFinder.Find();
            _address = new DnsEndPoint("127.0.0.1", _port, AddressFamily.InterNetwork);

            _testee = new ServiceListener(_port, 100, _gameServerCooridnator, _matchMaker);
            _testee.RunAsync().Wait();

            _client = new TcpClient();
            _client.Connect(_address.Host, _address.Port);
        }

        [Test]
        public void Test1()
        {
            string msg = "Hello broker!";
            Write(new Hello
            {
                Message = msg
            });

            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            StringAssert.AreEqualIgnoringCase(_matchMaker.GetHello(), msg);
        }

        private void Write(IMessage message)
        {
            var stream = _client.GetStream();

            using var binaryStream = new MemoryStream();
            message.WriteTo(binaryStream);

            var length = BitConverter.GetBytes((short) binaryStream.Length + Constants.HeaderSize);
            var tmp = length[0];
            length[0] = length[1];
            length[1] = tmp;

            stream.Write(length, 0, 2);    // length
            stream.Write(new byte[] {0, 0, 0, 0, 0, 0, 0, 0}, 0, 8);    // groupNo

            var typeHash = IPAddress.HostToNetworkOrder(StringHash.GetHashCode(message.Descriptor.FullName));
            stream.Write(BitConverter.GetBytes(typeHash), 0, 4);

            stream.Write(binaryStream.GetBuffer(), 0, (int) binaryStream.Length);
            stream.Flush();
        }
    }
}