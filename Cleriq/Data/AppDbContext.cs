using Microsoft.EntityFrameworkCore;
using Cleriq.Models;

namespace Cleriq.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComisieMembru>()
            .HasOne(cm => cm.Consilier)
            .WithMany(c => c.Apartenente)
            .HasForeignKey(cm => cm.ConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Prezenta>()
            .HasOne(p => p.Consilier)
            .WithMany(c => c.Prezente)
            .HasForeignKey(p => p.ConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vot>()
            .HasOne(v => v.Consilier)
            .WithMany(c => c.Voturi)
            .HasForeignKey(v => v.ConsilierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Institutie>().HasQueryFilter(e => !e.EsteSters);
        modelBuilder.Entity<Consilier>().HasQueryFilter(e => !e.EsteSters);
        modelBuilder.Entity<Comisie>().HasQueryFilter(e => !e.EsteSters);
        modelBuilder.Entity<ComisieMembru>().HasQueryFilter(e => !e.EsteSters);
        modelBuilder.Entity<Mandat>().HasQueryFilter(e => !e.EsteSters);
        modelBuilder.Entity<Sedinta>().HasQueryFilter(e => !e.EsteSters);
        modelBuilder.Entity<PunctOrdineZi>().HasQueryFilter(e => !e.EsteSters);
        modelBuilder.Entity<Prezenta>().HasQueryFilter(e => !e.EsteSters);
        modelBuilder.Entity<Vot>().HasQueryFilter(e => !e.EsteSters);
        modelBuilder.Entity<ProcesVerbal>().HasQueryFilter(e => !e.EsteSters);
    }

    public override int SaveChanges()
    {
        AplicaAuditSiSoftDelete();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AplicaAuditSiSoftDelete();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AplicaAuditSiSoftDelete()
    {
        var acum = DateTime.UtcNow;

        foreach (var intrare in ChangeTracker.Entries<EntitateDeBaza>())
        {
            switch (intrare.State)
            {
                case EntityState.Added:
                    intrare.Entity.CreatLa = acum;
                    break;

                case EntityState.Modified:
                    intrare.Entity.ModificatLa = acum;
                    break;

                case EntityState.Deleted:
                    intrare.State = EntityState.Modified;
                    intrare.Entity.EsteSters = true;
                    intrare.Entity.StersLa = acum;
                    break;
            }
        }
    }

    public DbSet<Institutie> Institutii { get; set; }
    public DbSet<Consilier> Consilieri { get; set; }
    public DbSet<Comisie> Comisii { get; set; }
    public DbSet<ComisieMembru> ComisieMembri { get; set; }
    public DbSet<Mandat> Mandate { get; set; }
    public DbSet<Sedinta> Sedinte { get; set; }
    public DbSet<PunctOrdineZi> PuncteOrdineZi { get; set; }
    public DbSet<Prezenta> Prezente { get; set; }
    public DbSet<Vot> Voturi { get; set; }
    public DbSet<ProcesVerbal> ProceseVerbale { get; set; }
}