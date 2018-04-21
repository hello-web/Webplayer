using Fastnet.Music.Messages;

namespace Fastnet.WebPlayer.Tasks
{
    public class LogitechPlayer //: AudioDevice
    {
        // these properties are only set as a result of server information call
        // they are not updated by the player information call
        // (this difference may be of value in the future!)
        public string MACAddress { get; set; }
        public string Name { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsPlaying { get; set; }
        public bool IsConnected { get; set; }
        public bool IsPowerOn { get; set; }
        public string UUID { get; set; }
    }
    public class LogitechPlayerStatus : LogitechPlayer
    {
        public double Duration { get; set; }
        public double Position { get; set; }
        public float Volume { get; set; }
        public string Mode { get; set; }
        public string Title { get; set; }
        public string File { get; set; }
    }
}
