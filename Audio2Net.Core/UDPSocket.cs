using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Audio2Net.Core
{
    public class UDPSocket
    {
        private UdpClient _udpClient;
        public UDPSocket(IPAddress serverIp, int serverPort)
        {
            _udpClient = new UdpClient(new IPEndPoint(serverIp, serverPort));
        }
        
        public UDPSocket()
        {
            _udpClient = new UdpClient();
        }

        public void Server(Action<IPEndPoint, byte[]> callback)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var result = await _udpClient.ReceiveAsync();
                    callback(result.RemoteEndPoint, result.Buffer);
                }
            });
        }

        public void Client(IPAddress ipAddress, int port, Action<IPEndPoint, byte[]> callback)
        {
            _udpClient.Connect(new IPEndPoint(ipAddress, port));
            Task.Run(async () =>
            {
                while (true)
                {
                    var result = await _udpClient.ReceiveAsync();
                    callback(result.RemoteEndPoint, result.Buffer);
                }
            });
        }

        public void Send(byte[] data, int length, IPEndPoint endPoint)
        {
            _udpClient.SendAsync(data, length, endPoint);
        }
        
        public void Send(byte[] data, int length)
        {
            _udpClient.SendAsync(data, length);
        }
    }
}