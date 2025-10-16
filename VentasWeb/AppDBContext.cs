using Microsoft.EntityFrameworkCore;
using VentasWeb.Models;

namespace VentasWeb;

public class AppDBContext(DbContextOptions<AppDBContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrecioForzado>(entity =>
        {
            entity.ToTable("Web_PreciosForzados", "dbo");
            entity.HasKey(a => a.Sku);
            entity.Property(a => a.PrecioLista).HasPrecision(18,4);
            entity.Property(a => a.PrecioVenta).HasPrecision(18,4);
        });

        modelBuilder.Entity<SucursalExcluida>(entity =>
        {
            entity.ToTable("Web_SucursalesExcluidas", "dbo");
            entity.HasKey(a => a.Numero);
        });

        modelBuilder.Entity<FamiliaExcluida>(entity =>
        {
            entity.ToTable("Web_FamiliasExcluidas", "dbo");
            entity.HasKey(a => a.Codigo);
        });
        modelBuilder.Entity<ArticuloExcluido>(entity =>
        {
            entity.ToTable("Web_ArticulosExcluidos", "dbo");
            entity.HasKey(a => a.SKU);
        });


        base.OnModelCreating(modelBuilder);
    }


    required public DbSet<PrecioForzado> PreciosForzados { get; set; }
    required public DbSet<SucursalExcluida> SucursalesExcluidas { get; set; }
    required public DbSet<FamiliaExcluida> FamiliasExcluidas { get; set; }
    required public DbSet<ArticuloExcluido> ArticulosExcluidos { get; set; }
}