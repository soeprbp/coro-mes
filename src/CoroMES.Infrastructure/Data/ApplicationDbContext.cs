using Microsoft.EntityFrameworkCore;
using CoroMES.Core.Entities;

namespace CoroMES.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Production
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderOperation> WorkOrderOperations => Set<WorkOrderOperation>();

    // Equipment
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentMaintenance> EquipmentMaintenances => Set<EquipmentMaintenance>();

    // Inventory
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<BillOfMaterials> BillOfMaterials => Set<BillOfMaterials>();
    public DbSet<MaterialMovement> MaterialMovements => Set<MaterialMovement>();

    // Workforce
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<LaborRecord> LaborRecords => Set<LaborRecord>();

    // Quality
    public DbSet<Inspection> Inspections => Set<Inspection>();
    public DbSet<InspectionItem> InspectionItems => Set<InspectionItem>();
    public DbSet<NonConformance> NonConformances => Set<NonConformance>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Alarms
    public DbSet<AlarmEvent> AlarmEvents => Set<AlarmEvent>();

    // Settings
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    // Displays
    public DbSet<DisplayDefinition> DisplayDefinitions => Set<DisplayDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // WorkOrder
        modelBuilder.Entity<WorkOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Number).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Number).IsUnique();
        });

        // Equipment
        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        modelBuilder.Entity<DisplayDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(40);
            entity.Property(e => e.SettingsJson).HasMaxLength(4000);
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(120);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.HasIndex(e => new { e.UserId, e.Key }).IsUnique();
            entity.HasIndex(e => e.Category);
        });

        // Material
        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(20);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Operator
        modelBuilder.Entity<Operator>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.EmployeeNumber).IsUnique();
        });

        // Shift
        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // Configure relationships
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Actor).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(80);
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Route).HasMaxLength(300);
            entity.Property(e => e.IpAddress).HasMaxLength(80);
            entity.Property(e => e.UserAgent).HasMaxLength(300);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(1000);
            entity.HasIndex(e => e.OccurredAtUtc);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
        });

        modelBuilder.Entity<AlarmEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.AlertChannels).HasMaxLength(300);
            entity.Property(e => e.NotificationSummary).HasMaxLength(1000);
            entity.Property(e => e.AcknowledgedBy).HasMaxLength(120);
            entity.Property(e => e.ResolvedBy).HasMaxLength(120);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Severity);
            entity.HasIndex(e => e.TriggeredAtUtc);
        });

        modelBuilder.Entity<WorkOrderOperation>()
            .HasOne(w => w.WorkOrder)
            .WithMany(w => w.Operations)
            .HasForeignKey(w => w.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EquipmentMaintenance>()
            .HasOne(e => e.Equipment)
            .WithMany(e => e.Maintenances)
            .HasForeignKey(e => e.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AlarmEvent>()
            .HasOne(e => e.Equipment)
            .WithMany()
            .HasForeignKey(e => e.EquipmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DisplayDefinition>()
            .HasOne(d => d.Equipment)
            .WithMany()
            .HasForeignKey(d => d.EquipmentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<BillOfMaterials>()
            .HasOne(b => b.Product)
            .WithMany(m => m.BillOfMaterialsItems)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LaborRecord>()
            .HasOne(l => l.Operator)
            .WithMany(o => o.LaborRecords)
            .HasForeignKey(l => l.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
