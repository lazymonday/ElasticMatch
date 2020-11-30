using DotNetty.Transport.Channels;
using Google.Protobuf;

namespace ElasticMatch
{
    public interface IServiceListenerContext
    {
        void Send<T>(T message) where T : IMessage<T>;

        IChannel GetChannel();
    }
}