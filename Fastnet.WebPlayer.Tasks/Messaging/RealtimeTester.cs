using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;

namespace Fastnet.WebPlayer.Tasks
{
    public class RealtimeTester : RealtimeTask
    {
        private CancellationToken cancellationToken;
        private readonly Messenger messenger;
        public RealtimeTester(Messenger messenger, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.messenger = messenger;
        }
        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            log.Trace($"{nameof(ExecuteAsync)}");
            this.cancellationToken = cancellationToken;
            //messenger.e
            await StartAsync();
        }
        private  Task StartAsync()
        {
            //await messenger.StartMulticastListener((m) =>
            //{
            //    //MulticastTest mt = m as MulticastTest;
            //    //log.Information($"Received {mt.Number}, {(mt.DateTimeUtc.ToString("ddMMMyyyy HH:mm:ss"))}");
            //});
            return Task.CompletedTask;
        }
    }
}
