using System;
using System.Threading;
using Audio2Net.Core.Interfaces;
using CSCore;
using CSCore.SoundOut;
using CSCore.Streams;

namespace Audio2Net.Core
{
    public class Client : IAudioManager
    {
        private Configuration _configuration;
        private ManualResetEvent _stopClient = new ManualResetEvent(false);
        private UDPSocket _udpSocket;

        public Client(Configuration configuration)
        {
            _configuration = configuration;
            _udpSocket = new UDPSocket();
        }

        public void Run()
        {
            while (true)
            {
                Console.WriteLine($"Starting client to {_configuration.IpAddress}:{_configuration.Port}...");
                RunClient();
                if (_stopClient.SafeWaitHandle.IsClosed)
                {
                    Console.WriteLine("Stopping...");
                    break;
                }
            }
        }

        private void RunClient()
        {
            // client mode
            using (var soundOut = new WasapiOut{ Latency = (int)(_configuration.MaxAudioLatency * 1000) })
            {
                var wavFormat = new WaveFormat(48000, 32, 2, AudioEncoding.IeeeFloat);
                using (var wb = new WriteableBufferingSource(wavFormat, (int)(wavFormat.BytesPerSecond * 
                                                                              _configuration.MaxAudioLatency)) 
                    { FillWithZeros = true })
                {
                    _udpSocket = new UDPSocket();
                    _udpSocket.Client(_configuration.IpAddress, _configuration.Port, (point, bytes) =>
                    {
                        wb.Write(bytes, 0, bytes.Length);
                    });
                    
                    _udpSocket.Send(new byte[]{0}, 1);
                    Console.WriteLine("Sent start request to server!");

                    soundOut.Initialize(wb);
                    soundOut.Play();

                    var paused = false;
                    while (true)
                    {
                        var key = Console.ReadKey();
                        switch (key.Key)
                        {
                            case ConsoleKey.Q:
                                soundOut.Stop();
                                _udpSocket.Send(new byte[]{1}, 1);
                                _stopClient.Close();
                                return;
                            case ConsoleKey.C:
                                _udpSocket.Send(new byte[]{0}, 1);
                                Console.WriteLine("Sending connection request to the server...");
                                break;
                            case ConsoleKey.P:
                                _udpSocket.Send(!paused ? new byte[] {1} : new byte[] {0}, 1);
                                paused = !paused;
                                break;
                            case ConsoleKey.R:
                                _udpSocket.Send(new byte[]{2}, 1);
                                _udpSocket.Send(new byte[]{1}, 1);
                                Console.WriteLine("Resetting server and client...");
                                return;
                            case ConsoleKey.X:
                                _udpSocket.Send(new byte[]{3}, 1);
                                Console.WriteLine("Stopping server and client...");
                                soundOut.Stop();
                                _stopClient.Close();
                                return;
                        }
                    }
                }
            }
        }
    }
}