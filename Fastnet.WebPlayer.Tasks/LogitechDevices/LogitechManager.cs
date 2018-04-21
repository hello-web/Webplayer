using System;
using System.Threading.Tasks;
using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;

namespace Fastnet.WebPlayer.Tasks
{
    public class LogitechManager : DeviceManager
    {
        //private bool respondToPulse;
        private string macAddress;
        private readonly ILogger log;
        private readonly LMSClient lmc;
        public LogitechManager(PlayerConfiguration playerConfiguration, string musicServerUrl, DeviceIdentifier identifier, Broadcaster broadcaster, ILoggerFactory lf) : base(playerConfiguration, musicServerUrl, identifier, broadcaster, lf)
        {
            this.macAddress = identifier.MACAddress;
            log = loggerFactory.CreateLogger<LogitechManager>();
            log.Information($"using device {identifier.DeviceName}");
            lmc = new LMSClient(playerConfiguration, this.loggerFactory);
        }
        public override async void Stop()
        {
            await lmc.Stop(macAddress);
            base.Stop();
        }
        protected override async Task JumpTo(PlayerCommand cmd)
        {
            if (isPlaying)
            {
                var lps = await lmc.PlayerInformation(macAddress);
                var required = lps.Duration * (cmd.Position / 100.0);
                await lmc.JumpTo(macAddress, required);
            }
        }

        protected override async Task Pause(PlayerCommand cmd)
        {
            await lmc.Pause(macAddress);
            isPaused = true;
        }

        protected override async Task Play(PlayerCommand cmd)
        {

            var url = $"{musicServerUrl}/{cmd.StreamUrl}";
            await lmc.Play(macAddress, url);
            isPlaying = true;
            var ds = new DeviceStatus
            {
                Identifier = this.identifier,
                State = Music.Core.DeviceState.Playing,
                PlaybackEvent = PlaybackEvent.PlayStarted,
                CurrentTime = TimeSpan.Zero,
                TotalTime = TimeSpan.Zero,
                //Volume = p.volume,
                PlaylistItemId = playlistEntry.ItemId,
                PlaylistSubItemId = playlistEntry.SubItemId
            };
            broadcaster.Queue(ds);
            log.Debug($"{url}  - playback started");
        }

        protected override async Task Resume(PlayerCommand cmd)
        {
            await lmc.Resume(macAddress);
            isPaused = false;
        }

        protected override async Task SetVolume(PlayerCommand cmd)
        {
            await lmc.SetVolume(macAddress, cmd.Volume);
        }
        protected override async void OnPulse()
        {
            var lps = await lmc.PlayerInformation(macAddress);
            log.Trace($"logitech player info: {lps.ToJson()}");
            var ds = new DeviceStatus
            {
                Identifier = this.identifier//,
            };
            switch (lps.Mode)
            {
                case "play":
                    ds.State = DeviceState.Playing;
                    ds.Volume = lps.Volume;
                    ds.TotalTime = TimeSpan.FromSeconds(lps.Duration);
                    ds.CurrentTime = TimeSpan.FromSeconds(lps.Position);
                    ds.PlaylistItemId = playlistEntry.ItemId;
                    ds.PlaylistSubItemId = playlistEntry.SubItemId;
                    broadcaster.Queue(ds);
                    break;
                case "pause":
                    ds.State = DeviceState.Paused;
                    ds.Volume = lps.Volume;
                    ds.TotalTime = TimeSpan.FromSeconds(lps.Duration);
                    ds.CurrentTime = TimeSpan.FromSeconds(lps.Position);
                    ds.PlaylistItemId = playlistEntry.ItemId;
                    ds.PlaylistSubItemId = playlistEntry.SubItemId;
                    broadcaster.Queue(ds);
                    break;
                case "stop":
                    if (isPlaying)
                    {
                        isPlaying = false;
                        isPaused = false;
                        ds = new DeviceStatus
                        {
                            Identifier = this.identifier,
                            State = Music.Core.DeviceState.Playing,
                            PlaybackEvent = PlaybackEvent.PlayStopped,
                            Volume = lps.Volume,
                            TotalTime = TimeSpan.FromSeconds(lps.Duration),
                            CurrentTime = TimeSpan.FromSeconds(lps.Position),
                            PlaylistItemId = playlistEntry.ItemId,
                            PlaylistSubItemId = playlistEntry.SubItemId
                        };
                        broadcaster.Queue(ds);
                    }
                    break;
            }

        }
    }
}
