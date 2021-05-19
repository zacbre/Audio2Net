using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Audio2Net.Core.Interfaces;
using CSCore;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.Streams;

namespace Audio2Net.Core
{
    public class Server : IAudioManager
    {
        private Configuration _configuration;
        private readonly List<IPEndPoint> _clients = new List<IPEndPoint>();
        private UDPSocket _udpSocket;
        private ManualResetEvent _resetEvent;

        public Server(Configuration configuration)
        {
            _configuration = configuration;
            _udpSocket = new UDPSocket(configuration.IpAddress, configuration.Port);
            _resetEvent = new ManualResetEvent(false);
        }

        public void Run()
        {
            var oldOutput = DeviceManager.CurrentDevice();
            Console.WriteLine($"Old output device is: {DeviceManager.GetDeviceNameById(oldOutput)}");
            if (_configuration.OutputDevice is { })
            {
                // Get current audio device
                if (int.TryParse(_configuration.OutputDevice, out var index))
                {
                    var output = DeviceManager.ListDevices().ToList()[index];
                    Console.WriteLine($"Setting output device to: {output.Key}");
                    if (output is {})
                    {
                        DeviceManager.ChangeDevice(output.Value);
                    }
                }
            }
            
            // server mode
            var stopServer = new ManualResetEvent(false);
            _udpSocket.Server((point, bytes) =>
            {
                if (bytes.Length == 1)
                {
                    switch (bytes[0])
                    {
                        case 0:
                            if (!_clients.Contains(point))
                            {
                                _clients.Add(point);
                                Console.WriteLine($"Got new client: {point.Address}:{point.Port}.");
                            }
                            break;
                        case 1:
                            if (_clients.Contains(point))
                            {
                                _clients.Remove(point);
                                Console.WriteLine($"Client left: {point.Address}:{point.Port}.");
                            }
                            break;
                        case 2:
                            Console.WriteLine("Resetting server...");
                            // Reset client and server.
                            _resetEvent.Set();
                            _resetEvent.Reset();
                            break;
                        case 3:
                            Console.WriteLine("Stopping server...");
                            // Stop server
                            stopServer.Close();
                            _resetEvent.Set();
                            break;
                    }
                }
            });
            
            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = false;
                stopServer.Close();
                _resetEvent.Set();
                if (oldOutput is {})
                {
                    DeviceManager.ChangeDevice(oldOutput);
                    Thread.Sleep(1000);
                }
            };

            while (true)
            {
                Console.WriteLine($"Starting server on {_configuration.IpAddress}:{_configuration.Port}...");
                RunServer(oldOutput);
                if (stopServer.SafeWaitHandle.IsClosed)
                {
                    Console.WriteLine("Stopping...");
                    break;
                }
            }
        }

        private void RunServer(string? oldOutput)
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            
            using (var wasapiCapture = new WasapiLoopbackCapture())
            {
                // List outputs
                wasapiCapture.Initialize();
                
                
                var device = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                var endpoint = AudioEndpointVolume.FromDevice(device);

                Console.WriteLine($"Audio Device: {device.FriendlyName}, Volume: {endpoint.MasterVolumeLevelScalar}");
                
                var wasapiCaptureSource = new SoundInSource(wasapiCapture) { FillWithZeros = false };
                IWaveSource waveSource = null;
                ChangeVolumeSource? changeVolumeSource = null;
                if (_configuration.ServerVolume)
                {
                    var sampleSource = wasapiCaptureSource.ToSampleSource().ChangeSampleRate(48000);
                    changeVolumeSource = sampleSource.ChangeVolume(endpoint.MasterVolumeLevelScalar *
                                                                   _configuration.ServerVolumeBoost);
                    waveSource = changeVolumeSource.ToStereo().ToWaveSource(32);
                }
                else
                {
                    waveSource = wasapiCaptureSource.ToSampleSource().ChangeSampleRate(48000).ToStereo().ToWaveSource(32);
                }
                
                var buffer = new byte[(int)(waveSource.WaveFormat.BytesPerSecond * _configuration.MaxAudioLatency)];
                wasapiCaptureSource.DataAvailable += (s, e) =>
                {
                    var read = waveSource.Read(buffer, 0, buffer.Length);
                    foreach (var client in _clients)
                    {
                        _udpSocket.Send(buffer, read, client);
                    }
                };
            
                Console.WriteLine("Starting capturer...");
                Console.WriteLine($"Bits Per Sample: {waveSource.WaveFormat.BitsPerSample}, " +
                                         $"Sample Rate: {waveSource.WaveFormat.SampleRate}, " +
                                         $"Channels: {waveSource.WaveFormat.Channels}, " +
                                         $"Encoding: {waveSource.WaveFormat.WaveFormatTag}");
            
                wasapiCapture.Start();

                var cancellationToken = new CancellationTokenSource();
                if (_configuration.ServerVolume)
                {
                    Task.Run(() =>
                    {
                        var oldVolume = 100f;
                        while (true)
                        {
                            var volumeCheck = endpoint.MasterVolumeLevelScalar;
                            if (Math.Abs(volumeCheck - oldVolume) > 0.0001f)
                            {
                                oldVolume = volumeCheck;
                                if (changeVolumeSource != null)
                                {
                                    changeVolumeSource.Volume = volumeCheck * _configuration.ServerVolumeBoost;
                                }
                                Console.WriteLine($"Volume Change: {volumeCheck} * {_configuration.ServerVolumeBoost} = " +
                                                  $"{Math.Round(volumeCheck * _configuration.ServerVolumeBoost, 2) * 100}% ");
                            }
                        
                            Thread.Sleep(500);
                        }
                    }, cancellationToken.Token);
                }

                _resetEvent.WaitOne();
            
                Console.WriteLine("Stopping capturer...");
                DeviceManager.ChangeDevice(oldOutput);
                Thread.Sleep(1000);

                wasapiCapture.Stop();
                wasapiCaptureSource.Dispose();
                cancellationToken.Cancel();
                endpoint.Dispose();
                waveSource.Dispose();
            }
        }
    }
}