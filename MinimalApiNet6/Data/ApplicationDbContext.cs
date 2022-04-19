using Microsoft.EntityFrameworkCore;
using MinimalApiNet6.Models;

namespace MinimalApiNet6.Data
{
    public class ApplicationDbContext : DbContext
    {   
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Patient> Patients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<Patient>()
                .Property(p => p.Name)
                .IsRequired()
                .HasColumnType("varchar(256)");

            modelBuilder.Entity<Patient>()
                .Property (p => p.Document)
                .IsRequired()
                .HasColumnType("varchar(15)");

            modelBuilder.Entity<Patient>()
                .ToTable("Patients");

            base.OnModelCreating(modelBuilder);
        }
    }
}
