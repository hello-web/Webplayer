using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fastnet.WebPlayer.Tasks
{
    public class AlternatePath
    {
        public string PathPrefix { get; set; }
        public string CorrespondingPath { get; set; }
    }
    public class PlayerConfiguration
    {
        public bool UseDefaultDeviceOnly { get; set; }
        public AudioDeviceType[] EnabledAudioTypes { get; set; }
        public bool CacheBeforePlaying { get; set; }
        public bool WasapiExclusiveMode { get; set; }
        public int WasapiLatency { get; set; }
        public bool TryAlternatePath { get; set; }
        public IEnumerable<AlternatePath> AlternatePaths { get; set; }
        public PlayerConfiguration()
        {
            CacheBeforePlaying = true; // temporarily make this the default - remove this feature altogether of url streaming proves to be working via the FilePlayer
            WasapiExclusiveMode = false; // exclusive mode simply does not work reliably ...
            WasapiLatency = 20;
            EnabledAudioTypes = new AudioDeviceType[] { AudioDeviceType.Wasapi };
        }
    }
}
