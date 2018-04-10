using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Fastnet.Webplayer.Data
{
    public enum CacheState
    {
        Downloading,
        Ready
    }
    public class CachedFile
    {
        public long Id { get; set; }
        [StringLength(36)]
        public string Uid { get; set; }
        [MaxLength(400)]
        public string DownloadedFilename { get; set; }
        public CacheState State { get; set; }
        public DateTimeOffset LastPlayedAt { get; set; }
        public DateTimeOffset DownloadStartedAt { get; set; }
        public DateTimeOffset DownloadFinishedAt { get; set; }
    }
}
