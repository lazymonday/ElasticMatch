using DotNetty.Transport.Channels;
using Google.Protobuf;

namespace ElasticMatch
{
    class ServiceListenerContext : IServiceListenerContext
    {
        private readonly IChannelHandlerContext _context;
        private readonly long _groupNo;

        public ServiceListenerContext(IChannelHandlerContext context, long groupNo)
        {
            _context = context;
            _groupNo = groupNo;
        }

        public void Send<T>(T message) where T : IMessage<T>
        {
            var buffer = _context.Allocator.Buffer(Constants.HeaderSize + message.CalculateSize());

            buffer.WriteLong(_groupNo);
            buffer.WriteInt(Util.StringHash.GetHashCode(message.Descriptor.FullName));
            buffer.WriteBytes(message.ToByteArray());

            _context.WriteAndFlushAsync(buffer);
        }

        public IChannel GetChannel()
        {
            return _context.Channel;
        }
    }
}