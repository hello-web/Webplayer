using Fastnet.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System.Linq;
using System.Threading;

namespace Fastnet.WebPlayer.Tasks
{
    public class WasapiManager : WindowsDeviceManager
    {
        public WasapiManager(PlayerConfiguration playerConfiguration, string musicServerUrl, DeviceIdentifier identifier,
            Broadcaster broadcaster, ILoggerFactory loggerFactory) : base(playerConfiguration, musicServerUrl, identifier, broadcaster, loggerFactory)
        {

        }
        protected override IWavePlayer GetDevice(MMDevice device)
        {
            AudioClientShareMode mode = playerConfiguration.WasapiExclusiveMode ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared;
            // AudioClientShareMode.Exclusive does not work when running in IIS - no idea why!!!
            //AudioClientShareMode mode = AudioClientShareMode.Shared;
            int latency = playerConfiguration.WasapiLatency;
            return new WasapiOut(mmDevice, mode, true, latency);
        }

    }
}
