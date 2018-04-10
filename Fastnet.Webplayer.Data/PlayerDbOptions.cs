using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fastnet.Webplayer.Data
{
    public class PlayerDbContextFactory : WebDbContextFactory
    {
        public PlayerDbContextFactory(IOptions<PlayerDbOptions> options, IServiceProvider sp) : base(options, sp)
        {
        }
    }
    public class PlayerDbOptions : WebDbOptions
    {
    }
    public class PlayerDbInitialiser
    {
        public static void Initialise(PlayerDb db)
        {
            var log = db.Database.GetService<ILogger<PlayerDb>>() as ILogger;
            var creator = db.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            var dbExists = creator.Exists();

            if (dbExists)
            {
                log.Debug("PlayerDb exists");
            }
            else
            {
                log.Warning("No PlayerDb found");
            }
            db.Database.Migrate();
            log.Trace("The following migrations have been applied:");
            var migrations = db.Database.GetAppliedMigrations();
            foreach (var migration in migrations)
            {
                log.Trace($"\t{migration}");
            }
            //db.Seed();
        }
    }
}
