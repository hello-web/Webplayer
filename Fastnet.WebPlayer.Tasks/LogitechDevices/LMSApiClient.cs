using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Fastnet.WebPlayer.Tasks
{
    public abstract class LMSApiClient : WebApiClient
    {
        protected readonly PlayerConfiguration playConfig;
        public LMSApiClient(PlayerConfiguration playConfig, ILoggerFactory loggerFactory) : base(playConfig.LogitechServerUrl, loggerFactory)
        {
            this.playConfig = playConfig;
        }
        protected async Task<T> PostJsonAsync<T>(string json)
        {
            JObject jo = JObject.Parse(json);
            var r = await this.PostJsonAsync<JObject, T>(GetJsonRpc(), jo);
            if(playConfig.TraceLMSApi)
            {
                log.Trace($"{json} send to {this.BaseAddress}");
            }
            return r;
        }
        protected async Task PostJsonAsync(string json)
        {
            JObject jo = JObject.Parse(json);
            await this.PostJsonAsync<JObject>(GetJsonRpc(), jo);
            if (playConfig.TraceLMSApi)
            {
                log.Trace($"{json} send to {this.BaseAddress}");
            }
            return;
        }
        private string GetJsonRpc()
        {
            return $"jsonrpc.js";
        }
    }
}
