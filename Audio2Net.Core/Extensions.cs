using System;
using CSCore;
using CSCore.DSP;
using CSCore.Win32;

namespace Audio2Net.Core
{
    public static class Extensions
    {
        public static ChangeVolumeSource ChangeVolume(
            this ISampleSource input,
            float volume)
        {
            if (input == null)
                throw new ArgumentNullException(nameof (input));

            return new ChangeVolumeSource(input) { Volume = volume };
        }
        
        public static bool Contains(this PropertyStore store, PropertyKey key)
        {
            for (int index = 0; index < store.Count; ++index)
            {
                PropertyKey propertyKey = store.GetKey(index);
                if (propertyKey.ID == key.ID && propertyKey.PropertyID == key.PropertyID)
                    return true;
            }
            return false;
        }
    }
}