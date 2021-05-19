using System.Text.Json.Serialization;
using CSCore.CoreAudioAPI;

namespace Audio2Net.Core
{
    public class DeviceFullInfo : DeviceInfo
    {
        public string IconPath { get; }
        public DeviceState State { get; }

        [JsonConstructor]
        public DeviceFullInfo(string name, string id, DataFlow type, string iconPath, DeviceState state, bool isUsb) : base(name, id, type, isUsb)
        {
            IconPath = iconPath;
            State = state;
        }

        public DeviceFullInfo(MMDevice device) : base(device)
        {
        }
    }
}