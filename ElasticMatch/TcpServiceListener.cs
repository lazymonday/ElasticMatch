using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Google.Protobuf;
using NLog;

namespace ElasticMatch
{
    public class TcpServiceListener : ChannelHandlerAdapter, IServiceListener
    {
        private int _port;
        private int _soBacklog;

        private ServerBootstrap _bootstrap = new ServerBootstrap();
        private IChannel _boundChannel;
        private ILogger _logger = LogManager.GetCurrentClassLogger();

        private Dictionary<int, Action<IServiceListenerContext, object>> _handlers =
            new Dictionary<int, Action<IServiceListenerContext, object>>();

        private Dictionary<int, MessageParser> _parsers = new Dictionary<int, MessageParser>();

        public TcpServiceListener(int port, int soBacklog)
        {
            _port = port;
            _soBacklog = soBacklog;
        }

        public override bool IsSharable => true;

        public async Task RunAsync()
        {
            IEventLoopGroup bossGroup = new MultithreadEventLoopGroup(1);
            IEventLoopGroup workerGroup = new MultithreadEventLoopGroup();

            _bootstrap.Group(bossGroup, workerGroup);
            _bootstrap.ChannelFactory(() => new TcpServerSocketChannel(System.Net.Sockets.AddressFamily.InterNetwork));

            _bootstrap
                .Option(ChannelOption.SoBacklog, _soBacklog)
                .Option(ChannelOption.SoReuseport, true)
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast("encoder", new LengthFieldPrepender(2));
                    pipeline.AddLast("decoder", new LengthFieldBasedFrameDecoder(ushort.MaxValue,
                        0, 2, 0, 2));
                    pipeline.AddLast(this);
                }));

            _boundChannel = await _bootstrap.BindAsync(_port);
        }

        public async Task StopAsync()
        {
            await _boundChannel.CloseAsync();
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);

            var remoteAddress = context.Channel.RemoteAddress.ToString();

            _logger.Info($"connected : {context.Channel.RemoteAddress.AddressFamily}/{remoteAddress}");

            if (context.Channel?.Active == false)
            {
                _logger.Error($"faulty connection channel : {remoteAddress}");
                context.CloseAsync();
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);

            var remoteAddress = context.Channel.RemoteAddress.ToString();

            _logger.Info($"disconnected : {context.Channel.RemoteAddress.AddressFamily}/{remoteAddress}");
        }

        protected void BindMessage<T>(Action<IServiceListenerContext, T> handler) where T : IMessage<T>
        {
            var type = typeof(T);
            IMessage nullMessage = (IMessage) Activator.CreateInstance(type);
            var typeHash = Util.StringHash.GetHashCode(nullMessage.Descriptor.FullName);

            MessageParser parser;
            try
            {
                parser =
                    type.GetProperty("Parser", BindingFlags.Static | BindingFlags.Public)?.GetValue(null, null) as
                        MessageParser<T>;
                if (parser == null)
                {
                    throw new ArgumentException();
                }
            }
            catch (Exception)
            {
                throw new ArgumentException($"It doesn't match message with protobuf : {type}");
            }

            _handlers.Add(typeHash, (context, arg) => handler(context, (T) arg));
            _parsers.Add(typeHash, parser);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (!(message is IByteBuffer buffer))
            {
                return;
            }

            if (buffer.ReadableBytes < Constants.HeaderSize)
            {
                return;
            }

            var groupNo = buffer.ReadLong();
            var typeHash = buffer.ReadInt();

            if (!_handlers.TryGetValue(typeHash, out var handler))
            {
                throw new ArgumentException($"Not registered packet : {typeHash}");
            }

            if (!_parsers.TryGetValue(typeHash, out var parser))
            {
                throw new InvalidOperationException("There is no parser but It has been registered.");
            }

            var serviceListenerContext = new ServiceListenerContext(context, groupNo);

            CodedInputStream cis;
            if (buffer.IoBufferCount <= 1) // is single buffer
            {
                ArraySegment<byte> ioBuffer = buffer.GetIoBuffer(buffer.ReaderIndex, buffer.ReadableBytes);
                cis = new CodedInputStream(ioBuffer.Array, ioBuffer.Offset, buffer.ReadableBytes);
            }
            else
            {
                Stream input1 = new ReadOnlyByteBufferStream(buffer, false);
                cis = new CodedInputStream(input1);
            }

            var packet = parser.ParseFrom(cis);
            handler(serviceListenerContext, packet);
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }
    }
}