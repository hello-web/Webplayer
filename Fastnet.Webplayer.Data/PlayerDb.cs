using Fastnet.Core.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fastnet.Webplayer.Data
{
    public class PlayerDb : WebDbContext
    {
        public PlayerDb(DbContextOptions<PlayerDb> options, IOptions<PlayerDbOptions> webDbOptions, IServiceProvider sp): base(options, webDbOptions, sp)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("play");
            modelBuilder.Entity<CachedFile>()
                .HasIndex(x => x.Uid)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
