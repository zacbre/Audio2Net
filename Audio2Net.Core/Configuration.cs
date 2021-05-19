using System.Net;

namespace Audio2Net.Core
{
    public class Configuration
    {
        public Configuration()
        {
            IpAddress = IPAddress.Any;
        }
        
        public IPAddress IpAddress { get; set; }
        public int Port { get; set; }
        public float MaxAudioLatency { get; set; }
        public bool ServerVolume { get; set; }
        
        public float ServerVolumeBoost { get; set; }
        public AudioManagerTypeEnum ManagerType { get; set; }
        public string? OutputDevice { get; set; }
    }
}