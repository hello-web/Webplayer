using Fastnet.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Webplayer.Controllers
{
    [Produces("application/json")]
    [Route("api/Test")]
    public class TestController : Controller
    {
        private readonly ILogger log;
        public TestController(ILogger<TestController> logger)
        {
            this.log = logger;
        }
        [HttpGet("mp3")]
        public IActionResult TestMp3()
        {
            var file = @"D:\Music\mp3\Western\Popular\Gerry Rafferty\City to City\Gerry Rafferty - 01 The Ark.mp3";
            using (var audioFile = new AudioFileReader(file))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(1000);
                }
            }
            return new EmptyResult();
        }
        [HttpGet("flac1")]
        public IActionResult TestFlac1()
        {
            var file = @"D:\Music\flac\Western\Popular\Gerry Rafferty\City to City\1978_077 Gerry Rafferty - Baker Street.flac";
            using (var audioFile = new AudioFileReader(file))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(1000);
                }
            }
            return new EmptyResult();
        }
        [HttpGet("flac2")]
        public IActionResult TestFlac2()
        {
            var file = @"D:\Music\flac\Western\Popular\Gerry Rafferty\City to City\1978_077 Gerry Rafferty - Baker Street.flac";
            using (var audioFile = new MediaFoundationReader(file))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(1000);
                }
            }
            return new EmptyResult();
        }
        [HttpGet("flac3")]
        public IActionResult TestFlac3()
        {
            var file = @"D:\Music\flac\Western\Popular\Gerry Rafferty\City to City\1978_077 Gerry Rafferty - Baker Street.flac";
            using (var audioFile = new MediaFoundationReader(file))
            using (var outputDevice = new WasapiOut())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(1000);
                }
            }
            return new EmptyResult();
        }
        [HttpGet("test1/{number}")]
        public IActionResult Test1Stream(int number)
        {
            var url = $"http://localhost:51776/test/stream/{number}";
            using (var mf = new MediaFoundationReader(url))
            {
                using (var wo = new WaveOutEvent())
                {
                    wo.Init(mf);
                    wo.Play();
                    while (wo.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            return new EmptyResult();
        }
        [HttpGet("test2/{number}")]
        public IActionResult Test2WasapiStream(int number)
        {
            var url = $"http://localhost:51776/test/stream/{number}";
            using (var mf = new MediaFoundationReader(url))
            {
                using (var wo = new WasapiOut())
                {
                    wo.Init(mf);
                    wo.Play();
                    while (wo.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            return new EmptyResult();
        }
        [HttpGet("test3/{number}")]
        public IActionResult Test3WasapiStream(int number)
        {
            var url = $"http://192.168.0.106:5700/test/stream/{number}";
            using (var mf = new MediaFoundationReader(url))
            {
                using (var wo = new WasapiOut())
                {
                    wo.Init(mf);
                    wo.Play();
                    while (wo.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            return new EmptyResult();
        }
        [HttpGet("list/devices")]
        public IActionResult ListDevices()
        {
            for (int n = -1; n < WaveOut.DeviceCount; n++)
            {
                var caps = WaveOut.GetCapabilities(n);
                log.LogInformation($"WaveOut: {n}: {caps.ProductName}");
            }
            foreach (var dev in DirectSoundOut.Devices)
            {
                log.LogInformation($"DirectSoundOut: {dev.Guid}; {dev.ModuleName}; {dev.Description}");
            }
            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                log.LogInformation($"wasapi: {wasapi.DataFlow}; {wasapi.FriendlyName}; {wasapi.DeviceFriendlyName}; {wasapi.State}");
            }
            foreach (var asio in AsioOut.GetDriverNames())
            {
                log.LogInformation($"AsioOut: {asio}");
            }
            return new EmptyResult();
        }
        [HttpGet("wasapi/devices")]
        public IActionResult ListWasapiDevices()
        {
            //for (int n = -1; n < WaveOut.DeviceCount; n++)
            //{
            //    var caps = WaveOut.GetCapabilities(n);
            //    log.LogInformation($"WaveOut: {n}: {caps.ProductName}");
            //}
            //foreach (var dev in DirectSoundOut.Devices)
            //{
            //    log.LogInformation($"DirectSoundOut: {dev.Guid}; {dev.ModuleName}; {dev.Description}");
            //}
            var enumerator = new MMDeviceEnumerator();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
            {
                try
                {
                    log.LogInformation($"wasapi: {wasapi.DataFlow}; {wasapi.FriendlyName}; {wasapi.DeviceFriendlyName}; {wasapi.State}");
                }
                catch (Exception xe)
                {
                    log.Error($"some MMDevice caused error: {xe.Message}");
                }
            }
            //foreach (var asio in AsioOut.GetDriverNames())
            //{
            //    log.LogInformation($"AsioOut: {asio}");
            //}
            return new EmptyResult();
        }

    }
}
