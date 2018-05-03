using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.WebPlayer.Tasks
{
    public abstract class DeviceManager: IDisposable
    {
        protected (long ItemId, long SubItemId) playlistEntry;
        protected bool isPlaying; // this is the palying or stopped state
        protected bool isPaused; // this is the play/paused toggle

        internal string LocalStore { get; set; }
        private readonly ILogger log;
        private long currentlyPlayingMusicFileId;
        protected DeviceIdentifier identifier;
        public CancellationTokenSource CancellationSource { get; private set; }
        protected readonly string musicServerUrl;
        protected readonly Broadcaster broadcaster;
        protected PlayerConfiguration playerConfiguration;
        protected readonly ILoggerFactory loggerFactory;
        public DeviceManager(PlayerConfiguration playerConfiguration, string musicServerUrl, DeviceIdentifier identifier,
             Broadcaster broadcaster, ILoggerFactory lf)
        {
            this.playerConfiguration = playerConfiguration;
            this.musicServerUrl = musicServerUrl;
            this.loggerFactory = lf;
            this.log = loggerFactory.CreateLogger<DeviceManager>();
            this.identifier = identifier;
            this.broadcaster = broadcaster;
            log.Information($"player configuration: {playerConfiguration.ToJson()}");
        }
        public virtual void ReplacePlayConfiguration(PlayerConfiguration pc)
        {
            this.playerConfiguration = pc;
        }
        public virtual void CommandHandler(PlayerCommand cmd)
        {
            // Note: PlayerCommands.Stop is intercepted by the Receiver task (q.v. PlayerCommandHandler)
            // the Receiver then calls the StopDeviceAsync method of the DeviceManagerFactory
            // this is so that the factory can clean up its records of running DeviceManagers
            // eventually the Stop() method here is called (but remember it is normally overriden by 
            // concrete DeviceManager instances.
            switch (cmd.Command)
            {
                case PlayerCommands.Play:
                    playlistEntry = (cmd.PlaylistItemId, cmd.PlaylistSubItemId);
                    currentlyPlayingMusicFileId = cmd.MusicFileId;
                    Play(cmd);
                    break;
                case PlayerCommands.TogglePlayPause:
                    if (isPlaying)
                    {
                        if (isPaused)
                        {
                            Resume(cmd);
                        }
                        else
                        {
                            Pause(cmd);
                        } 
                    }
                    else
                    {
                        log.Warning($"received toggleplaypause when isPlaying is false!");
                    }
                    break;
                case PlayerCommands.JumpTo:
                    JumpTo(cmd);
                    break;
                case PlayerCommands.SetVolume:
                    SetVolume(cmd);
                    break;
            }
        }
        public async Task StartAsync()
        {
            CancellationSource = new CancellationTokenSource();
            var taskFactory = new TaskFactory(TaskScheduler.Current);
            await taskFactory.StartNew(async () => await Run(), CancellationSource.Token) ;           
        }
        public virtual void Stop()
        {
            CancellationSource.Cancel();
            WaitHandle wh = CancellationSource.Token.WaitHandle;
            wh.WaitOne(10000);
            Dispose();
            log.Debug($"{this.GetType().Name} disposed");
        }
        protected abstract Task Play(PlayerCommand cmd);
        protected abstract Task Pause(PlayerCommand cmd);
        protected abstract Task Resume(PlayerCommand cmd);
        protected abstract Task JumpTo(PlayerCommand cmd);
        protected abstract Task SetVolume(PlayerCommand cmd);
        protected abstract void OnPulse(DeviceStatus ds);
        //protected virtual void OnPulse()
        //{
        //    log.Debug($"{identifier.DeviceName}: 1 second pulse");
        //}
        private async Task Run()
        {
            while(!CancellationSource.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000);
                    var ds = new DeviceStatus
                    {
                        Identifier = this.identifier,
                        MusicFileId = currentlyPlayingMusicFileId                                                   
                    };
                    OnPulse(ds);
                }
                catch (AggregateException)
                {
                    log.Information($"AggregateException");
                }
                catch (TaskCanceledException)
                {
                    log.Information($"TaskCanceledException");
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                }
            }
            log.Debug($"task cancelled");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DeviceManager() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion


    }
}
