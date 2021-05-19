using System;
using CSCore;

namespace Audio2Net.Core
{
    public class ChangeVolumeSource : SampleAggregatorBase
    {
        public ChangeVolumeSource(ISampleSource source) : base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
        }
        
        public virtual float Volume { get; set; }

        public override int Read(float[] buffer, int offset, int count)
        {
            int length = base.Read(buffer, offset, count);
            float volume = Volume;
            if (volume == 0.0 || volume > -9.99999974737875E-05 && volume < 9.99999974737875E-05)
            {
                Array.Clear(buffer, offset, length);
            }
            else if (volume != 1.0 && (volume <= 0.999899983406067 || volume >= 1.00010001659393))
            {
                for (int index = offset; index < length + offset; ++index)
                {
                    buffer[index] *= volume;
                }
            }

            return length;
        }
    }
}