﻿using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.WebPlayer.Tasks
{
    // Some *important* experiences with NAudio MediaFoundationReader behaviour
    // 1. Until this morning (8Apr2018) I failed to find a method that would start a stream without first downloading the entire file
    //    thus causing a delay between a the paly command and the music starting, especially with large flac files and across the LAN
    // 2. using the StreamMediaFoundationReader did not help as I was not able to pass it the Url, I had to create a MemoryStream
    //    and then fill its buffer - and therefore suffer the download delay
    // 3. I started writing code that would support caching of music files. This entailed a local db to kepp track of ongoing downloads
    //    and records of what had been previously cached. This stuff is incomplete.
    // 4. I then though about playing a file over the network using unc paths and added the TryAlternatePath feature. This stuff is does not work (yet?) as
    //    I do not pass the full name of the music file in the Play command (only a url to use to start a FileStream) - hence there isn't the information
    //    to substitute an alternate path. I am no longer this stuff will be necessary - see (5)
    // 5. As a result of trying to get (4) working I accidently passed the streamurl directly to the FilePlayer (rather than the StreamPlayer)
    //    without downloading the music data and creating a local file in music.cache. The result was that teh file stream started playing
    //    immediately without needing a download first!!! 
    // 6. Early days yet (8Apr2018) but perhaps I don't need StreamPlayer, I don't need a any pre downlaoding of a music file into music.cache and I don't need
    //    any loacl db?? Early days becuase it remains to be shown that
    //        a. This is reliable approach (ie. using a stream url as a the source to FilePlayer)
    //        b. Playing across the network via stream url does not cause glitches due to network traffic.
    // 7. To avoid confusion in the future if (6) turns out to be the solution, I need to refactor stuff to get rid of the current version of StreamPlayer and replace it with one that
    //    uses MediaFoundation with a source url (providing a FileStream) and without any prior download.
    public abstract class WindowsDeviceManager : DeviceManager
    {
        public abstract class MediaFoundationPlayer
        {
            public event EventHandler<EventArgs> PlaybackStarting;
            public event EventHandler<EventArgs> PlaybackStopped;
            protected WaveStream reader;
            protected SampleChannel channel;
            protected readonly ILogger log;
            protected readonly IWavePlayer player;
            protected readonly string musicSource;
            protected readonly PlayerCommand cmd;
            public PlayerConfiguration PlayerConfiguration { get; set; }
            public MediaFoundationPlayer(string source, PlayerCommand cmd, IWavePlayer player, ILogger log)
            {
                this.musicSource = source;
                this.cmd = cmd;
                this.log = log;
                this.player = player;
            }
            public abstract Task PlayAsync();
            public abstract void Pause();
            public abstract void Resume();
            public virtual void Stop()
            {

            }
            public (NAudio.Wave.PlaybackState playbackState, TimeSpan currentTime, TimeSpan totalTime, float volume) GetProgress()
            {
                return (player.PlaybackState, reader.CurrentTime, reader.TotalTime, channel.Volume);
            }
            public void Dispose()
            {
                try
                {
                    if (reader != null)
                    {
                        reader.Dispose();
                    }
                    if (player != null)
                    {
                        player.Dispose();
                    }
                }
                catch (Exception xe)
                {
                    log.Error(xe);
                }
            }
            protected virtual void RaisePlaybackStarting()
            {
                PlaybackStarting?.Invoke(this, new EventArgs { });
            }
            protected virtual void RaisePlaybackStopped()
            {
                PlaybackStopped?.Invoke(this, new EventArgs { });
            }
        }
        public class FilePlayer : MediaFoundationPlayer
        {
            private string musicFile;
            private readonly string localStore;
            public FilePlayer(string source, PlayerCommand cmd, string localStore, IWavePlayer player, ILogger log) : base(source, cmd, player, log)
            {
                this.localStore = localStore;
            }
            public override Task PlayAsync()
            {
                //if (PlayerConfiguration.TryAlternatePath)
                //{
                //    musicFile = FindAlternatePath(musicSource);
                //}
                //else
                //{
                //    musicFile = await DownloadFile(musicSource, cmd.EncodingType, cmd.MusicFileUid);
                //}
                musicFile = musicSource;
                PlayFile();
                return Task.CompletedTask;
            }
            public override void Pause()
            {
                player?.Pause();
            }
            public override void Resume()
            {
                player?.Play();
            }
            private void PlayFile()
            {
                log.Debug($"playing {musicFile}");
                try
                {
                    reader = new MediaFoundationReader(musicFile);
                    //player = GetDevice(mmDevice);
                    channel = new SampleChannel(reader, true);
                    channel.Volume = 0.5f;
                    player.PlaybackStopped += (o, e) =>
                    {
                        //File.Delete(musicFile);
                        musicFile = null;
                        //Player_PlaybackStopped(o, e);
                        RaisePlaybackStopped();
                    };
                    player.Init(reader);
                    //isPlaying = true;
                    RaisePlaybackStarting();
                    player.Play();
                }
                catch (System.Exception xe)
                {
                    log.Error(xe);
                }
            }
            private async Task<string> DownloadFile(string url, EncodingType encodingType, string uid)
            {
                using (var ta = new TimedAction((ts) =>
                {
                    log.Debug($"download time {ts.ToString()}");
                }))
                {
                    string extension;// = null; //".mp3";
                    switch (encodingType)
                    {
                        case EncodingType.flac:
                            extension = ".flac";
                            break;
                        default:
                            extension = ".mp3";
                            break;
                    }
                    var uniqueName = Path.Combine(localStore, uid + extension);
                    var wc = new WebClient();
                    log.Debug($"downloading from {url} to {uniqueName}");
                    await wc.DownloadFileTaskAsync(url, uniqueName);
                    return uniqueName;
                }
            }
            private string FindAlternatePath(string musicSource)
            {
                foreach (var item in PlayerConfiguration.AlternatePaths)
                {
                    if (musicSource.StartsWith(item.PathPrefix, StringComparison.CurrentCultureIgnoreCase))
                    {
                        var replacement = musicSource.Replace(item.PathPrefix, item.CorrespondingPath);
                        log.Debug($"Replacing {musicSource} with {replacement}");
                        return replacement;
                    }
                }
                return musicSource;
            }
        }
        public class StreamPlayer : MediaFoundationPlayer
        {
            private WebClient wc;
            private MemoryStream stream;
            private Stream webStream;
            private byte[] buffer;
            private int bytesRead;
            public StreamPlayer(string source, PlayerCommand cmd, IWavePlayer player, ILogger log) : base(source, cmd, player, log)
            {

            }
            public override Task PlayAsync()
            {
                log.Debug($"playing stream {musicSource}");
                try
                {
                    stream = GetSourceStream();
                    reader = new StreamMediaFoundationReader(stream);
                    channel = new SampleChannel(reader, true);
                    channel.Volume = 0.5f;
                    player.PlaybackStopped += (o, e) =>
                    {
                        stream.Dispose();
                        wc.Dispose();
                        RaisePlaybackStopped();
                    };
                    player.Init(reader);
                    RaisePlaybackStarting();
                    player.Play();
                }
                catch (System.Exception xe)
                {
                    log.Error(xe);
                }
                return Task.CompletedTask;
            }
            public override void Pause()
            {
                
            }
            public override void Resume()
            {
                
            }
            private MemoryStream GetSourceStream()
            {
                wc = new WebClient();
                using (var ta = new TimedAction((ts) =>
                {
                    log.Debug($"download time {ts.ToString()}");
                }))
                {
                    stream = new MemoryStream(wc.DownloadData(musicSource));
                }
                return stream;
            }
            private MemoryStream GetBufferedSourceStream()
            {
                wc = new WebClient();
                using (var ta = new TimedAction((ts) =>
                {
                    log.Debug($"download time {ts.ToString()}");
                }))
                {
                    buffer = new byte[8192 * 2];
                    //int counter = 0;
                    webStream = wc.OpenRead(musicSource);
                    stream = new MemoryStream();
                    bytesRead = webStream.Read(buffer, 0, buffer.Length);
                    stream.Write(buffer, 0, bytesRead);
                    Task.Run(() =>
                    {
                        while (bytesRead > 0)
                        {
                            bytesRead = webStream.Read(buffer, 0, buffer.Length);
                            stream.Write(buffer, 0, bytesRead);
                        }
                    });
                }
                IMFByteStream bs = MediaFoundationApi.CreateByteStream(stream);
                return stream;
            }
        }
        protected readonly MMDevice mmDevice;
        

        private readonly ILogger log;
        private MediaFoundationPlayer mfp;
        public WindowsDeviceManager(PlayerConfiguration playerConfiguration, string musicServerUrl, DeviceIdentifier identifier,
            Broadcaster broadcaster, ILoggerFactory loggerFactory) : base(playerConfiguration, musicServerUrl, identifier, broadcaster, loggerFactory)
        {
            log = loggerFactory.CreateLogger<WindowsDeviceManager>();
            var enumerator = new MMDeviceEnumerator();
            mmDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .SingleOrDefault(x => x.FriendlyName == identifier.DeviceName);
            if (mmDevice == null)
            {
                log.Error($"{identifier.DeviceName} not found");
            }
            else
            {
                log.LogInformation($"using device {mmDevice.FriendlyName}, {mmDevice.DeviceFriendlyName}");
            }
        }
        public override void ReplacePlayConfiguration(PlayerConfiguration pc)
        {
            base.ReplacePlayConfiguration(pc);
            if (mfp != null)
            {
                mfp.PlayerConfiguration = pc;
            }
        }
        public override void Stop()
        {
            log.Debug("Stop() called");
            mfp?.Stop();
            // Important! base.Stop() must be called
            base.Stop();
        }
        protected override void Pause(PlayerCommand cmd)
        {
            mfp?.Pause();
            isPaused = true;
        }
        protected override void Resume(PlayerCommand cmd)
        {
            mfp?.Resume();
            isPaused = false;
        }
        protected override Task Play(PlayerCommand cmd)
        {
            if (mmDevice != null)
            {
                var url = $"{musicServerUrl}/{cmd.StreamUrl}";
                log.Debug($"request to call {url}");
                mfp?.Dispose();
                if (playerConfiguration.CacheBeforePlaying)
                {
                    mfp = new FilePlayer(url, cmd, LocalStore, GetDevice(mmDevice), loggerFactory.CreateLogger<FilePlayer>());
                }
                else
                {
                    mfp = new StreamPlayer(url, cmd, GetDevice(mmDevice), loggerFactory.CreateLogger<StreamPlayer>());
                }
                mfp.PlayerConfiguration = playerConfiguration;
                mfp.PlaybackStarting += Mfp_PlaybackStarting;
                mfp.PlaybackStopped += Mfp_PlaybackStopped;
                mfp.PlayAsync();
            }
            return Task.CompletedTask;
        }
        private void Mfp_PlaybackStopped(object sender, EventArgs e)
        {
            isPlaying = false;
            var p = mfp.GetProgress();
            mfp?.Dispose();
            var ds = new DeviceStatus
            {
                Identifier = this.identifier,
                State = Music.Messages.PlaybackState.Stopped,
                CurrentTime = p.currentTime,
                TotalTime = p.totalTime,
                Volume = p.volume
            };
            broadcaster.Queue(ds);
        }

        private void Mfp_PlaybackStarting(object sender, EventArgs e)
        {
            isPlaying = true;
        }
        //protected async  void PlayOld(PlayerCommand cmd)
        //{
        //    if (mmDevice != null)
        //    {
        //        try
        //        {
        //            playingDownloadedFile = false;
        //            downloadedFilename = null;
        //            var url = $"{musicServerUrl}/{cmd.StreamUrl}";
        //            log.Debug($"request to call {url}");
        //            Reset();
        //            var source = url;
        //            if (playerConfiguration.CacheBeforePlaying)
        //            {
        //                playingDownloadedFile = true;
        //                downloadedFilename = await DownloadFile(url, cmd.EncodingType);
        //                PlayFile();
        //            }
        //            else
        //            {
        //                PlayStream(source);
        //            }
        //        }
        //        catch (Exception xe)
        //        {
        //            log.Error(xe);
        //            //throw;
        //        }
        //    }
        //}
        //private async Task<string> DownloadFile(string url, EncodingType encodingType)
        //{
        //    string extension;// = null; //".mp3";
        //    switch (encodingType)
        //    {
        //        case EncodingType.flac:
        //            extension = ".flac";
        //            break;
        //        default:
        //            extension = ".mp3";
        //            break;
        //    }
        //    var uniqueName = Path.Combine(LocalStore, Guid.NewGuid().ToString() + extension);
        //    var wc = new WebClient();
        //    log.Debug($"downloading from {url} to {uniqueName}");
        //    await wc.DownloadFileTaskAsync(url, uniqueName);
        //    return uniqueName;
        //}
        //private void PlayStream(string source)
        //{
        //    log.Debug($"playing stream {source}");
        //    try
        //    {
        //        var wc = new WebClient();
        //        MemoryStream stream = null;
        //        using (var ta = new TimedAction((ts) =>
        //        {
        //            log.Debug($"download time {ts.ToString()}");
        //        }))
        //        {
        //            stream = new MemoryStream(wc.DownloadData(source));
        //        }
        //        reader = new StreamMediaFoundationReader(stream);
        //        player = GetDevice(mmDevice);
        //        channel = new SampleChannel(reader, true);
        //        channel.Volume = 0.5f;
        //        player.PlaybackStopped += (o, e) =>
        //        {
        //            stream.Dispose();
        //            wc.Dispose();
        //            //Reset();
        //            Player_PlaybackStopped(o, e);
        //        };
        //        player.Init(reader);
        //        isPlaying = true;
        //        player.Play();
        //    }
        //    catch (System.Exception xe)
        //    {
        //        log.Error(xe);
        //    }
        //}
        //private void PlayFile()
        //{
        //    log.Debug($"playing {downloadedFilename}");
        //    try
        //    {
        //        reader = new MediaFoundationReader(downloadedFilename);
        //        player = GetDevice(mmDevice);
        //        channel = new SampleChannel(reader, true);
        //        channel.Volume = 0.5f;
        //        player.PlaybackStopped += (o, e) =>
        //        {
        //            File.Delete(downloadedFilename);
        //            downloadedFilename = null;
        //            Player_PlaybackStopped(o, e);
        //        };
        //        player.Init(reader);
        //        isPlaying = true;
        //        player.Play();
        //    }
        //    catch (System.Exception xe)
        //    {
        //        log.Error(xe);
        //    }
        //}
        //private void Reset()
        //{
        //    try
        //    {
        //        isPlaying = false;
        //        if (reader != null)
        //        {
        //            reader.Dispose();
        //        }
        //        if (player != null)
        //        {
        //            player.Dispose();
        //        }
        //    }
        //    catch (Exception xe)
        //    {
        //        log.Error(xe);
        //    }
        //}
        protected abstract IWavePlayer GetDevice(MMDevice device);
        protected override void OnPulse()
        {
            if (isPlaying)
            {
                var p = mfp.GetProgress();
                var ds = new DeviceStatus
                {
                    Identifier = this.identifier//,
                    //State = p.playbackState
                };
                switch(p.playbackState)
                {
                    case NAudio.Wave.PlaybackState.Paused:
                        ds.State = Music.Messages.PlaybackState.Paused;
                        break;
                    case NAudio.Wave.PlaybackState.Playing:
                        ds.State = Music.Messages.PlaybackState.Playing;
                        break;
                    case NAudio.Wave.PlaybackState.Stopped:
                        ds.State = Music.Messages.PlaybackState.Stopped;
                        break;
                }
                switch (ds.State)
                {
                    case Music.Messages.PlaybackState.Playing:
                    case Music.Messages.PlaybackState.Paused:
                        ds.CurrentTime = p.currentTime;
                        ds.TotalTime = p.totalTime;
                        ds.Volume = p.volume;
                        break;
                }
                if(ds.State == Music.Messages.PlaybackState.NotKnown || isPlaying && ds.State == Music.Messages.PlaybackState.Stopped
                    || !isPlaying && (ds.State == Music.Messages.PlaybackState.Playing || ds.State == Music.Messages.PlaybackState.Paused
                    || isPlaying && isPaused && ds.State != Music.Messages.PlaybackState.Paused))
                {
                    log.Warning($"{this.identifier.DeviceName} inconsistent state, isPlaying = {isPlaying}, isPaused = {isPaused}, ds.State = {ds.State.ToString()}");
                }
                broadcaster.Queue(ds);
            }
        }
        protected override void Dispose(bool disposing)
        {
            mfp?.Dispose();
            base.Dispose(disposing);
        }
    }
}


