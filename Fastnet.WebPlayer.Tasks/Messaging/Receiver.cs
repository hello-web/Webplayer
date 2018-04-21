using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.WebPlayer.Tasks
{
    public class Receiver : RealtimeTask
    {
        private int bulletinCount = 0;
        private CancellationToken cancellationToken;
        private readonly Messenger messenger;
        private readonly MusicServerInformation serverInformation;
        private readonly string machineName;
        private readonly DeviceManagerFactory dmf;
        private readonly MusicConfiguration musicConfig;
        private readonly PlayerConfiguration playerConfiguration;
        private readonly string currentIpAddress;
        public Receiver(IOptions<PlayerConfiguration> playerConfigOptions, IOptions<MusicConfiguration> musicConfigOptions, DeviceManagerFactory dmf, Messenger messenger, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.playerConfiguration = playerConfigOptions.Value;
            this.musicConfig = musicConfigOptions.Value;
            this.dmf = dmf;
            this.messenger = messenger;
            serverInformation = new MusicServerInformation();
            machineName = Environment.MachineName.ToLower();
            var list = NetInfo.GetMatchingIPV4Addresses(musicConfig.LocalCIDR);
            currentIpAddress = list.First().ToString();
        }

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            log.Trace($"{nameof(ExecuteAsync)}");
            this.cancellationToken = cancellationToken;
            messenger.AddMulticastSubscription<AllPointsBulletin>((m) => APBHandler(m as AllPointsBulletin));
            messenger.AddMulticastSubscription<PlayerCommand>(async (m) => await PlayerCommandHandler(m as PlayerCommand));
            while (!this.cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(10000);
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                }
            }
            if (cancellationToken.IsCancellationRequested)
            {
                log.Debug($"CancellationRequested");
            }
            return;
        }

        private async Task PlayerCommandHandler(PlayerCommand playerCommand)
        {
            if (playerCommand.Identifier.HostMachine == machineName)
            {
                if (playerCommand.Command == PlayerCommands.Stop)
                {
                    dmf.StopDeviceAsync(playerCommand.Identifier);
                }
                else
                {
                    var dm = await dmf.GetManagerAsync(playerCommand.Identifier);
                    dm?.CommandHandler(playerCommand);
                }
            }
        }

        private void APBHandler(AllPointsBulletin bulletin)
        {
            var (result, serverUrl) = CanReachServer(bulletin.ServerInformation.Url);
            if (result)
            {
                bulletinCount++;
                if (this.serverInformation.MachineName != bulletin.ServerInformation.MachineName)
                {
                    this.serverInformation.MachineName = bulletin.ServerInformation.MachineName;
                    log.Information($"music server is on {this.serverInformation.MachineName}");
                }
                if (this.serverInformation.Url != serverUrl)
                {
                    this.serverInformation.Url = serverUrl;
                    this.dmf.SetMusicServerUrl(this.serverInformation.Url);
                    log.Information($"music server url is {this.serverInformation.Url}");
                }
                if ((bulletinCount % 5) == 0)
                {
                    //log.Debug($"{bulletinCount} bulletins received");
                }
            }
        }
        private (bool, string) CanReachServer(string url)
        {
            if (playerConfiguration.SwapLocalIpAddressToLocalHost)
            {
                bool result = false;
                var uri = new Uri(url);
                if (currentIpAddress == uri.Host)
                {
                    // we are on the ip address as the music server
                    url = $"http://localhost:{uri.Port}";
                    //log.Warning($"music server url changed to {url}");
                    result = true;
                }
                else
                {
                    result = true;
                }
                return (result, url);
            }
            return (true, url);
        }
    }
}
