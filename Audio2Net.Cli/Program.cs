using System;
using System.Net;
using Audio2Net.Core;
using Audio2Net.Core.Interfaces;

namespace Audio2Net.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = ParseCommandLineArgs(args);
            if (configuration is null)
            {
                return;
            }

            IAudioManager manager = configuration.ManagerType switch
            {
                AudioManagerTypeEnum.Client => new Client(configuration),
                AudioManagerTypeEnum.Server => new Server(configuration),
                _                           => throw new ArgumentOutOfRangeException()
            };

            manager.Run();
        }

        private static Configuration? ParseCommandLineArgs(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine("Usage: ./Audio2Net.exe <--c|--s> --ip [ip] --port [port] [--max-latency] " +
                                         "[0-100] [--server-volume] [--volume--boost] [0-1]] [--output] [device] [--list-outputs]");
                return null;
            }

            string? ipAddress = null, port = null, latencySeconds = "1", serverVolumeLevel = "1", output = null;
            AudioManagerTypeEnum? managerType = null;
            bool serverVolume = false;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--c":
                        managerType = AudioManagerTypeEnum.Client;
                        break;
                    case "--s":
                        managerType = AudioManagerTypeEnum.Server;
                        break;
                    case "--ip":
                        if (i+1 >= args.Length)
                        {
                            Console.WriteLine("Please specify the IP by passing --ip [ip]");
                            return null;
                        }
                        ipAddress = args[++i];
                        break;
                    case "--port":
                        if (i+1 >= args.Length)
                        {
                            Console.WriteLine("Please specify the IP by passing --ip [ip]");
                            return null;
                        }
                        port = args[++i];
                        break;
                    case "--max-latency":
                        if (i+1 >= args.Length)
                        {
                            Console.WriteLine("Please specify the latency in seconds by passing --max-latency " +
                                                     "[latency]");
                            return null;
                        }
                        latencySeconds = args[++i];
                        break;
                    case "--server-volume":
                        Console.WriteLine("Letting server control volume...");
                        serverVolume = true;
                        break;
                    case "--volume-boost":
                        if (i+1 >= args.Length)
                        {
                            Console.WriteLine("Please specify the volume by passing --volume-boost [multiplier] " +
                                                     "(1 = 100%, 0.1 = 10%, 0.01 = 1%)");
                            return null;
                        }
                        serverVolume = true;
                        serverVolumeLevel = args[++i];
                        Console.WriteLine($"Setting audio volume to: {serverVolumeLevel}");
                        break;
                    case "--output":
                        if (i+1 >= args.Length)
                        {
                            Console.WriteLine("Please specify the output device by passing --output [device]." +
                                                     "You can get a list of devices by passing: --list-outputs");
                            return null;
                        }
                        output = args[++i];
                        break;
                    case "--list-outputs":
                        Console.WriteLine("List of Output Devices:");
                        var item = 0;
                        foreach (var (deviceName, val) in DeviceManager.ListDevices())
                        {
                            Console.WriteLine($"[{item}] {deviceName} - {val}");
                            item++;
                        }
                        Environment.Exit(0);
                        break;
                };
            }

            return ValidateArgs(ipAddress, port, managerType, latencySeconds, serverVolume, 
                serverVolumeLevel, output);
        }
        
        private static Configuration? ValidateArgs(string? ipAddress, string? port, AudioManagerTypeEnum? audioManagerType, 
                                                   string? latencySeconds, bool serverVolume, string? serverVolumeBoost,
                                                   string? output)
        {
            if (audioManagerType == null)
            {
                Console.WriteLine("Please specify a server or client type with --s or --c");
                return null;
            }

            if (audioManagerType == AudioManagerTypeEnum.Client && (ipAddress == null || port == null))
            {
                Console.WriteLine("Please specify an IP and port to connect the client to.");
                return null;
            }

            if (audioManagerType == AudioManagerTypeEnum.Server && port == null)
            {
                Console.WriteLine("Please specify a port to use for the server with --port [port].");
                return null;
            }

            IPAddress parsedIpAddress;
            if (audioManagerType == AudioManagerTypeEnum.Server && string.IsNullOrEmpty(ipAddress))
            {
                parsedIpAddress = IPAddress.Any;
            }
            else
            {
                if (!IPAddress.TryParse(ipAddress, out parsedIpAddress))
                {
                    Console.WriteLine($"Failed to parse the IP Address '{ipAddress}'!");
                    return null;
                }
            }
            
            if (!int.TryParse(port, out var parsedPort))
            {
                Console.WriteLine($"Failed to parse the port '{port}'!");
                return null;
            }
            
            if (!float.TryParse(latencySeconds, out var latencySecondsFloat))
            {
                Console.WriteLine($"Failed to parse the latency seconds '{latencySeconds}'!");
                return null;
            }

            if (!float.TryParse(serverVolumeBoost, out var serverVolumeBoostFloat))
            {
                Console.WriteLine($"Failed to parse the server volume boost '{serverVolumeBoost}'!");
            }
            
            if (output is {} && output.Contains("\""))
            {
                output = output.Trim('"');
            }

            return new Configuration
            {
                IpAddress = parsedIpAddress,
                Port = parsedPort,
                ManagerType = audioManagerType.Value,
                ServerVolume = serverVolume,
                MaxAudioLatency = latencySecondsFloat,
                ServerVolumeBoost = serverVolumeBoostFloat,
                OutputDevice = output
            };
        }
    }
}