using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.WebPlayer.Tasks
{
    public class Broadcaster : RealtimeTask
    {
        private CancellationToken cancellationToken;
        private readonly Messenger messenger;
        private readonly PlayerConfiguration playConfig;
        private readonly MusicConfiguration musicConfig;
        private WebPlayerInformation webPlayerInformation;
        private BlockingCollection<MessageBase> messageQueue;
        private int webPlayerBroadcastInterval;
        private DeviceStatus penUltimateStatus;
        public Broadcaster(IOptions<PlayerConfiguration> playerConfigOptions, IOptions<MusicConfiguration> musicConfigOptions, Messenger messenger, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.messenger = messenger;
            this.playConfig = playerConfigOptions.Value;
            this.musicConfig = musicConfigOptions.Value;
            webPlayerBroadcastInterval = musicConfig.WebPlayerBroadcastInterval;
            InitialiseQueue();
        }
        public void SetWebPlayerBroadcastIntervalLong()
        {
            webPlayerBroadcastInterval = 10000;// musicConfig.WebPlayerBroadcastInterval;
        }
        public void SetWebPlayerBroadcastIntervalShort()
        {
            webPlayerBroadcastInterval = musicConfig.WebPlayerBroadcastInterval;
        }
        public void Queue(MessageBase message)
        {
            if(message is DeviceStatus)
            {
                var ds = message as DeviceStatus;
                if(penUltimateStatus != null)
                {
                    if (ds.State != penUltimateStatus.State)
                    {
                        log.Debug($"Device id {ds.Identifier.DeviceId}, state changed from {penUltimateStatus.State} to {ds.State}");
                    }
                }
                else
                {
                    log.Debug($"Device id {ds.Identifier.DeviceId}, state starts as {ds.State}");
                }
                penUltimateStatus = ds;
            }
            messageQueue.Add(message);
        }
        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            log.Trace($"{nameof(ExecuteAsync)}");
            this.cancellationToken = cancellationToken;
            InitialiseWebPlayerInformation();
            messenger.EnableMulticastSend();
            messenger.DiscardMessage<WebPlayerInformation>();
            messenger.DiscardMessage<DeviceStatus>();
            await StartAsync();
        }
        private void InitialiseQueue()
        {
            messageQueue = new BlockingCollection<MessageBase>();
            Task.Run(async () => { await ServiceQueue(); });
        }
        private async Task ServiceQueue()
        {
            while (!messageQueue.IsCompleted)
            {
                MessageBase message = null;
                try
                {
                    message = messageQueue.Take();
                }
                catch (InvalidOperationException) { }
                catch (Exception xe)
                {
                    log.Error(xe);
                }
                if (message != null)
                {
                    try
                    {
                        await messenger.SendMulticastAsync(message);
                        //if (!(message is WebPlayerInformation))
                        //{
                        //    log.Debug($"multicast {message.GetType().Name} sent");
                        //}
                    }
                    catch (Exception xe)
                    {
                        log.Error(xe);
                    }
                }
            }
        }
        private async Task StartAsync()
        {
            int deviceListUpdateCounter = 0;
            while (!this.cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Queue(webPlayerInformation);
                    await Task.Delay(webPlayerBroadcastInterval);
                    deviceListUpdateCounter++;
                    if ((deviceListUpdateCounter % 3) == 0)
                    {
                        UpdateDeviceList();
                        deviceListUpdateCounter = 0;
                    }
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                    //throw;
                }
            }
            if (cancellationToken.IsCancellationRequested)
            {
                log.Debug($"CancellationRequested");
            }
        }
        private void UpdateDeviceList()
        {
            var list = new List<AudioDevice>();
            foreach(var audioType in playConfig.EnabledAudioTypes)
            {
                switch(audioType)
                {
                    case AudioDeviceType.Asio:
                        // asio was originally designed for one device only
                        // here I take it to be the first device and mark that as the default one
                        foreach (var asio in AsioOut.GetDriverNames())
                        {
                            //log.Information($"AsioOut: {asio}");
                            list.Add(new AudioDevice { Type = AudioDeviceType.Asio, Name = asio });
                            if(playConfig.UseDefaultDeviceOnly && list.Count(x => x.Type == AudioDeviceType.Asio) == 1)
                            {
                                break;
                            }
                        }
                        var fd = list.FirstOrDefault(x => x.Type == AudioDeviceType.Asio);
                        if(fd != null)
                        {
                            fd.IsDefault = true;
                        }
                        break;
                    case AudioDeviceType.DirectSoundOut:
                        foreach (var dev in DirectSoundOut.Devices)
                        {
                            bool msd = dev.Guid == DirectSoundOut.DSDEVID_DefaultPlayback;
                            list.Add(new AudioDevice { Type = AudioDeviceType.DirectSoundOut, Name = dev.Description, IsDefault = msd });
                            if(playConfig.UseDefaultDeviceOnly && msd)
                            {
                                break;
                            }
                        }
                        break;
                    case AudioDeviceType.Wasapi:
                        var enumerator = new MMDeviceEnumerator();
                        var de = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                        list.Add(new AudioDevice { Type = AudioDeviceType.Wasapi, Name = de.FriendlyName, IsDefault = true });
                        if (!playConfig.UseDefaultDeviceOnly)
                        {
                            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, NAudio.CoreAudioApi.DeviceState.Active))
                            {
                                if (wasapi.FriendlyName != de.FriendlyName)
                                {
                                    list.Add(new AudioDevice { Type = AudioDeviceType.Wasapi, Name = wasapi.FriendlyName });
                                }
                            }
                        }
                        break;
                    case AudioDeviceType.Logitech:
                        log.Warning("Logitech devices not implemented yet");
                        break;
                }
            }
            webPlayerInformation.AudioDevices = list;
        }
        private void InitialiseWebPlayerInformation()
        {
            var list = NetInfo.GetMatchingIPV4Addresses(musicConfig.LocalCIDR);
            if (list.Count() > 1)
            {
                log.Warning($"Multiple local ipaddresses: {(string.Join(", ", list.Select(l => l.ToString()).ToArray()))}, cidr is {musicConfig.LocalCIDR}, config error?");
            }
            var ipAddress = list.First();
            webPlayerInformation = new WebPlayerInformation
            {
                MachineName = Environment.MachineName.ToLower(),
                Url = $"http://{ipAddress.ToString()}:{musicConfig.WebplayerPort}"
            };
            UpdateDeviceList();
        }
    }
}
