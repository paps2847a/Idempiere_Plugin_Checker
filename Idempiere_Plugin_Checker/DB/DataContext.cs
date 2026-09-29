using Microsoft.EntityFrameworkCore;
using Idempiere_Plugin_Checker.Models;

namespace Idempiere_Plugin_Checker.DB;

public class DataContext : DbContext
{
    public DataContext()
    {
    }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<Ambiente> Ambientes => Set<Ambiente>();
    public DbSet<PluginData> Plugins => Set<PluginData>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<KeyUsersAmbiente> KeyUsersAmbientes => Set<KeyUsersAmbiente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración Fluent API para Ambiente
        modelBuilder.Entity<Ambiente>(entity =>
        {
            entity.ToTable("Ambientes");

            entity.HasKey(e => e.IdAmb);
            entity.Property(e => e.IdAmb).ValueGeneratedOnAdd();

            entity.Property(e => e.NamAmb)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.DirAmb)
                .IsRequired()
                .HasMaxLength(300);

            entity.Property(e => e.RegDat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsAct)
                .HasDefaultValue(true);

            entity.HasMany(e => e.Plugins)
                .WithOne(p => p.Ambiente)
                .HasForeignKey(p => p.IdAmb)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.KeyUsers)
                .WithOne(k => k.Ambiente)
                .HasForeignKey(k => k.IdAmb)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración Fluent API para PluginData
        modelBuilder.Entity<PluginData>(entity =>
        {
            entity.ToTable("Plugins");

            // Clave primaria compuesta (Id del bundle + Id del ambiente)
            entity.HasKey(e => new { e.Id, e.IdAmb });

            entity.Property(e => e.Name)
                .HasMaxLength(255);

            entity.Property(e => e.SymbolicName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Version)
                .HasMaxLength(100);

            entity.Property(e => e.State)
                .HasMaxLength(50);

            entity.Property(e => e.Category)
                .HasMaxLength(100);

            entity.Property(e => e.SubidoPor)
                .HasMaxLength(100);

            // Índices para optimizar búsquedas frecuentes
            entity.HasIndex(e => e.SymbolicName);
            entity.HasIndex(e => e.IdAmb);
            entity.HasIndex(e => e.State);

            // Ignorar propiedades calculadas auxiliares para que no se mapeen como columnas
            entity.Ignore(e => e.ResolvedSubidoPor);
            entity.Ignore(e => e.ResolvedUploadDate);
            entity.Ignore(e => e.IsActive);
        });

        // Configuración Fluent API para AdminUser
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("AdminUsers");

            entity.HasKey(e => e.IdUsr);
            entity.Property(e => e.IdUsr).ValueGeneratedOnAdd();

            entity.Property(e => e.UsrNam)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.UsrPass)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.RegDat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsAct)
                .HasDefaultValue(true);

            entity.HasIndex(e => e.UsrNam).IsUnique();
        });

        // Configuración Fluent API para KeyUsersAmbiente
        modelBuilder.Entity<KeyUsersAmbiente>(entity =>
        {
            entity.ToTable("KeyUsersAmbientes");

            // Clave primaria compuesta (Usuario + Ambiente)
            entity.HasKey(e => new { e.IdUsr, e.IdAmb });

            entity.Property(e => e.UsrNam)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.UsrPass)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.RegDat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsAct)
                .HasDefaultValue(true);
        });

        // Seed Data Inicial para que la interfaz muestre los datos de la maqueta
        SeedInitialData(modelBuilder);
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        var fixedDate = new DateTime(2026, 9, 15, 12, 0, 0);

        modelBuilder.Entity<AdminUser>().HasData(
            new AdminUser
            {
                IdUsr = 1,
                UsrNam = "admin",
                UsrPass = "admin123",
                RegDat = fixedDate,
                IsAct = true
            }
        );

        // modelBuilder.Entity<Ambiente>().HasData(
        //     new Ambiente { IdAmb = 1, NamAmb = "Ambiente 1", DirAmb = "https://192.168.6.107:8443", RegDat = fixedDate, IsAct = true },
        //     new Ambiente { IdAmb = 2, NamAmb = "Ambiente 2", DirAmb = "https://192.168.6.108:8443", RegDat = fixedDate, IsAct = true },
        //     new Ambiente { IdAmb = 3, NamAmb = "Ambiente 3", DirAmb = "https://192.168.6.109:8443", RegDat = fixedDate, IsAct = true },
        //     new Ambiente { IdAmb = 4, NamAmb = "Ambiente 4", DirAmb = "https://192.168.6.110:8443", RegDat = fixedDate, IsAct = true }
        // );

        // modelBuilder.Entity<PluginData>().HasData(
        //     // Ambiente 1
        //     new PluginData
        //     {
        //         Id = 101,
        //         IdAmb = 1,
        //         Name = "GBot Custom Extensions",
        //         SymbolicName = "com.gbot.custom",
        //         Version = "12.0.0.PR20260915164128",
        //         State = "Active",
        //         StateRaw = 32,
        //         Category = "idempiere-plugin",
        //         Fragment = false,
        //         SubidoPor = null
        //     },
        //     new PluginData
        //     {
        //         Id = 102,
        //         IdAmb = 1,
        //         Name = "Ingeint Requisition Management",
        //         SymbolicName = "com.ingeint.requisition",
        //         Version = "12.0.0.JG20260810142010",
        //         State = "Active",
        //         StateRaw = 32,
        //         Category = "idempiere-plugin",
        //         Fragment = false,
        //         SubidoPor = null
        //     },
        //     // Ambiente 2
        //     new PluginData
        //     {
        //         Id = 101,
        //         IdAmb = 2,
        //         Name = "GBot Custom Extensions",
        //         SymbolicName = "com.gbot.custom",
        //         Version = "12.0.0.PR20260915164128",
        //         State = "Active",
        //         StateRaw = 32,
        //         Category = "idempiere-plugin",
        //         Fragment = false,
        //         SubidoPor = null
        //     },
        //     new PluginData
        //     {
        //         Id = 103,
        //         IdAmb = 2,
        //         Name = "Ingeint Transport Management",
        //         SymbolicName = "com.ingeint.tms",
        //         Version = "12.0.0.AL20260705093015",
        //         State = "Active",
        //         StateRaw = 32,
        //         Category = "idempiere-plugin",
        //         Fragment = false,
        //         SubidoPor = null
        //     },
        //     new PluginData
        //     {
        //         Id = 104,
        //         IdAmb = 2,
        //         Name = "Payment Gateway Integration",
        //         SymbolicName = "com.idempiere.payment.gateway",
        //         Version = "12.0.0.PR20260920110000",
        //         State = "Active",
        //         StateRaw = 32,
        //         Category = "idempiere-plugin",
        //         Fragment = false,
        //         SubidoPor = null
        //     },
        //     // Ambiente 3
        //     new PluginData
        //     {
        //         Id = 105,
        //         IdAmb = 3,
        //         Name = "Electronic Invoicing Fiscal",
        //         SymbolicName = "org.idempiere.fiscal.einvoice",
        //         Version = "12.0.0.MC20260830174500",
        //         State = "Active",
        //         StateRaw = 32,
        //         Category = "idempiere-plugin",
        //         Fragment = false,
        //         SubidoPor = null
        //     },
        //     new PluginData
        //     {
        //         Id = 106,
        //         IdAmb = 3,
        //         Name = "Warehouse Barcode Scanner",
        //         SymbolicName = "com.idempiere.wms.scanner",
        //         Version = "11.0.0.CR20260512101530",
        //         State = "Active",
        //         StateRaw = 32,
        //         Category = "idempiere-plugin",
        //         Fragment = false,
        //         SubidoPor = null
        //     },
        //     // Ambiente 4
        //     new PluginData
        //     {
        //         Id = 107,
        //         IdAmb = 4,
        //         Name = "Advanced Manufacturing MRP",
        //         SymbolicName = "com.idempiere.mrp.advanced",
        //         Version = "12.0.0.PR20260901083000",
        //         State = "Active",
        //         StateRaw = 32,
        //         Category = "idempiere-plugin",
        //         Fragment = false,
        //         SubidoPor = null
        //     }
        // );

        // modelBuilder.Entity<KeyUsersAmbiente>().HasData(
        //     new KeyUsersAmbiente
        //     {
        //         IdUsr = 1,
        //         IdAmb = 1,
        //         UsrNam = "prierasystem",
        //         UsrPass = "Pepeman56-",
        //         RegDat = fixedDate,
        //         IsAct = true
        //     }
        // );
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=DbMain.db");
        }
        base.OnConfiguring(optionsBuilder);
    }
}
