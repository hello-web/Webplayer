using Fastnet.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fastnet.WebPlayer.Tasks
{

    public class LMSClient : LMSApiClient
    {
        private class LogitechMediaServerStatus
        {
            public class Result
            {
                //public int __invalid_name__other player count { get; set; }
                //public int __invalid_name__info total albums { get; set; }
                [JsonProperty("player count")]
                public int count { get; set; }
                public string version { get; set; }
                [JsonProperty("players_loop")]
                public List<LogitechMediaServerStatus.PlayerRawData> PlayerList { get; set; }
                public string uuid { get; set; }
                //public int __invalid_name__sn player count { get; set; }
                //public int __invalid_name__info total artists { get; set; }
                //public int __invalid_name__info total songs { get; set; }
                public string lastscan { get; set; }
                //public int __invalid_name__info total genres { get; set; }
            }
            public class PlayerRawData
            {
                public int seq_no { get; set; }
                //[JsonProperty("playerid")]
                public string playerid { get; set; }
                public string displaytype { get; set; }
                public int connected { get; set; }
                //[JsonProperty("ip")]
                public string ip { get; set; }
                //[JsonProperty("model")]
                public string model { get; set; }
                //[JsonProperty("name")]
                public string name { get; set; }
                public string uuid { get; set; }
                public int isplayer { get; set; }
                public int isplaying { get; set; }
                public int canpoweroff { get; set; }
                public int power { get; set; }
                public string modelname { get; set; }
            }
            public List<object> @params { get; set; }
            public string method { get; set; }
            public int id { get; set; }
            [JsonProperty("result")]
            public Result Info { get; set; }
        }

        public LMSClient(PlayerConfiguration playConfig, ILoggerFactory loggerFactory) : base(playConfig, loggerFactory)
        {

        }
        private async Task SendCommand(string macAddress, string command)
        {
            
            string json = $@"{{""id"":1,""method"":""slim.request"",""params"":[""{macAddress}"",[""{command}""]]}}";
            await PostJsonAsync(json);
        }
        public async Task SetVolume(string macAddress, double level)
        {
            //level = level * 100.0;
            string json = $@"{{""id"":1,""method"":""slim.request"",""params"":[""{macAddress}"",[""mixer"",""volume"",{(level.ToString("00"))}]]}}";
            await PostJsonAsync(json);
        }
        public async Task JumpTo(string macAddress, double position)
        {
            //{"id":1,"method":"slim.request","params":["00:04:20:23:cc:b5",["time",221.97196261682242]]}
            string json = $@"{{""id"":1,""method"":""slim.request"",""params"":[""{macAddress}"",[""time"",{(position.ToString("0.000"))}]]}}";
            await PostJsonAsync(json);
        }
        public async Task Resume(string macAddress)
        {
            await SendCommand(macAddress, "play");
        }
        public async Task Pause(string macAddress)
        {
            await SendCommand(macAddress, "pause");
        }
        public async Task Stop(string macAddress)
        {
            await SendCommand(macAddress, "stop");
        }
        public async Task<List<LogitechPlayer>> ServerInformationAsync()
        {
            string json = @"{""id"":1,""method"":""slim.request"",""params"":["""",[""serverstatus"",0,999]]}";
            List<LogitechPlayer> players = new List<LogitechPlayer>();
            try
            {
                LogitechMediaServerStatus root = await PostJsonAsync<LogitechMediaServerStatus>(json);// await this.PostJsonAsync<JObject, LogitechMediaServerStatus>(GetJsonRpc(), jo);
                if (root?.Info.PlayerList != null)
                {
                    foreach (var item in root.Info.PlayerList)
                    {
                        var rd = item.ToJson();
                        log.Trace($"item: {rd}");
                        players.Add(new LogitechPlayer
                        {
                            //UUID = Guid.Parse(item.uuid).ToString(),
                            MACAddress = item.playerid,
                            Name = item.name,
                            IsPlayer = item.isplayer == 1,
                            IsPlaying = item.isplaying == 1,
                            IsConnected = item.connected == 1,
                            IsPowerOn = item.power == 1,
                            ModelName = item.modelname
                        });
                    }
                }
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
            return players;// root;
        }
        public async Task Play(string macAddress, string url)
        {
            string query = $"anyurl?p0=playlist&p1=play&p2={url}&player={macAddress}";
            await GetAsync(query);
        }
        public async Task<LogitechPlayerStatus> PlayerInformation(string macAddress)
        {
            try
            {
                //{""id"":1,""method"":""slim.request"",""params"":[""00:04:20:12:4b:2b"",[""status"",""-"",1,""tags:uB""]]}
                string json = string.Format(@"{{""id"":1,""method"":""slim.request"",""params"":[""{0}"",[""status"",""-"",1,""tags:uB""]]}}", macAddress);
                //var response = await PostJsonStringAsync(json);
                //var root = await PostJsonAsync<PlayerStatusRootObject>(json);
                JObject droot = await PostJsonAsync<dynamic>(json);
                if (droot != null)
                {
                    var result = droot["result"];
                    LogitechPlayerStatus status = null;
                    try
                    {

                        status = new LogitechPlayerStatus
                        {
                            //UUID = device.DeviceId,
                            Mode = result.Value<string>("mode"),
                            Name = result.Value<string>("player_name"),
                            Duration = result.Value<double>("duration"),
                            Position = result.Value<double>("time"),
                            Volume = result.Value<int>("mixer volume"),
                        };
                        //if (result["playlist_loop"] != null)
                        //{
                        //    JArray loop = (JArray)result["playlist_loop"];
                        //    status.File = new Uri(loop[0].Value<string>("url")).LocalPath;
                        //    status.Title = loop[0].Value<string>("title");
                        //}
                        //if(root.Result.PlaylistItem != null)
                        //{
                        //    //status.PlaylistLength = root.Result.PlaylistItem.Count;
                        //    //status.Title = root.Result.PlaylistItem?.Title;
                        //    //status.File = new Uri(root.Result.PlaylistItem?.Url).ToString();
                        //}
                    }
                    catch (Exception)
                    {
                        Debugger.Break();
                        //throw;
                    }
                    return status;// root;
                }
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
            return null;
        }
    }
}
