using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using System.Collections.Generic;

namespace Fastnet.WebPlayer.Tasks
{
    public class DeviceComparer : IEqualityComparer<DeviceIdentifier>
    {
        public bool Equals(DeviceIdentifier x, DeviceIdentifier y)
        {
            return x.DeviceId == y.DeviceId;
        }

        public int GetHashCode(DeviceIdentifier obj)
        {
            return obj.DeviceId.GetHashCode();
        }
    }
}
