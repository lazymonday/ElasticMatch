using System.Net;
using System.Net.Sockets;

static class FreeBindablePortFinder
{
    public static int Find()
    {
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            return ((IPEndPoint) socket.LocalEndPoint).Port;
        }
    }
}