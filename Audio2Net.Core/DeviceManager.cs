using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CSCore.CoreAudioAPI;
using SoundSwitch.Audio.Manager;
using SoundSwitch.Audio.Manager.Interop.Enum;

namespace Audio2Net.Core
{
    public class DeviceManager
    {
        private static MMDeviceEnumerator _mmDeviceEnumerator = new MMDeviceEnumerator();
        
        public static string? GetDeviceIdByName(string? name)
        {
            var items = ListDevices();
            return items.FirstOrDefault(p => p.Key == name).Value;
        }
        
        public static string GetDeviceNameById(string? id)
        {
            var items = ListDevices();
            return items.FirstOrDefault(p => p.Value == id).Key;
        }
        
        public static Dictionary<string,string> ListDevices()
        {
            var deviceList = new Dictionary<string, string>();
            foreach (var device in _mmDeviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active))
            {
                var deviceInfo = new DeviceFullInfo(device);
                deviceList.Add(device.FriendlyName, deviceInfo.Id);
            }
            
            return deviceList;
        }

        public static void ChangeDevice(string? deviceId)
        {
            AudioSwitcher.Instance.SwitchTo(deviceId, ERole.eMultimedia);
        }

        public static string? CurrentDevice()
        {
            var defaultMm = _mmDeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (defaultMm is { })
            {
                var deviceInfo = new DeviceFullInfo(defaultMm);
                return deviceInfo.Id;
            }

            return null;
        }
    }
}