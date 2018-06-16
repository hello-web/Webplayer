using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.WebPlayer.Tasks
{
    public class DeviceManagerFactory
    {
        /// <summary>
        /// There is a separate instance of a concrete DeviceManager per device
        /// </summary>
        private readonly Dictionary<DeviceIdentifier, DeviceManager> managers;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger log;
        private string musicServerUrl;
        private readonly SchedulerService schedulerService;
        private PlayerConfiguration playerConfiguration;
        private readonly IHostingEnvironment env;
        public DeviceManagerFactory(IHostingEnvironment env, IOptionsMonitor<PlayerConfiguration> playerConfigurationOptions,
            IHostedService hs, ILoggerFactory lf)
        {
            this.env = env;
            this.schedulerService = hs as SchedulerService;
            managers = new Dictionary<DeviceIdentifier, DeviceManager>(new DeviceComparer());
            loggerFactory = lf;
            log = loggerFactory.CreateLogger<DeviceManagerFactory>();
            musicServerUrl = null;
            this.playerConfiguration = playerConfigurationOptions.CurrentValue;
            playerConfigurationOptions.OnChangeWithDelay((opt) =>
            {
                this.playerConfiguration = opt;
                foreach(var item in managers)
                {
                    item.Value.ReplacePlayConfiguration(this.playerConfiguration);
                }
            });
        }
        public void SetMusicServerUrl(string url)
        {
            this.musicServerUrl = url;
        }
        public async Task<DeviceManager> GetManagerAsync(DeviceIdentifier identifier)
        {
            if (!string.IsNullOrWhiteSpace(musicServerUrl))
            {
                if (!managers.ContainsKey(identifier))
                {
                    DeviceManager dm = null;
                    var broadcaster = this.schedulerService.GetRealtimeTask<Broadcaster>();
                    switch (identifier.Type)
                    {
                        //case AudioDeviceType.Asio:
                        //    dm = new AsioManager(musicServerUrl, identifier, lf.CreateLogger<AsioManager>());
                        //    break;
                        //case AudioDeviceType.DirectSoundOut:
                        //    dm = new DirectSoundManager(musicServerUrl, identifier, lf.CreateLogger<DirectSoundManager>());
                        //    break;
                        case AudioDeviceType.Wasapi:
                            dm = new WasapiManager(playerConfiguration, musicServerUrl, identifier, broadcaster, loggerFactory);
                            break;
                        case AudioDeviceType.Logitech:
                            dm = new LogitechManager(playerConfiguration, musicServerUrl, identifier, broadcaster, loggerFactory);
                            break;
                    }
                    dm.LocalStore = Path.Combine(env.ContentRootPath, "music.cache");
                    if (!Directory.Exists(dm.LocalStore))
                    {
                        Directory.CreateDirectory(dm.LocalStore);
                    }
                    await dm.StartAsync();
                    managers.Add(identifier, dm);
                    broadcaster.SetWebPlayerBroadcastIntervalLong();
                    log.Debug($"{dm.GetType().Name} started");
                }
                return managers[identifier];
            }
            else
            {
                log.Warning($"cannot access any device managers without a music server url");
                return null;
            }
        }
        public void StopDeviceAsync(DeviceIdentifier identifier)
        {
            if (managers.ContainsKey(identifier))
            {
                var dm = managers[identifier];
                dm.Stop();
                managers.Remove(identifier);
                if (managers.Count() == 0)
                {
                    var broadcaster = this.schedulerService.GetRealtimeTask<Broadcaster>();
                    broadcaster.SetWebPlayerBroadcastIntervalShort();
                }
                log.Debug($"{dm.GetType().Name} stopped");
            }
            else
            {
                log.Debug($"Device Manager for {identifier.DeviceName} not found");
            }
        }
    }
}
