using Ahk.GradeManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data
{
    public class AhkDb : DbContext
    {
        internal const string DatabaseName = "ahk";
        internal const string ContainerName = "ahkgrades";

        private static volatile bool isCreated = false;

        public AhkDb(DbContextOptions<AhkDb> options)
            : base(options)
        {
        }

        public DbSet<WebhookToken> WebhookTokens { get; set; }
        public DbSet<StudentResult> Results { get; set; }

        public async ValueTask EnsureCreated()
        {
            if (isCreated)
                return;

            await this.Database.EnsureCreatedAsync();
            isCreated = true;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultContainer(ContainerName);

            modelBuilder.Entity<WebhookToken>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<StudentResult>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();
                e.OwnsMany(x => x.Points);
            });
        }
    }
}
