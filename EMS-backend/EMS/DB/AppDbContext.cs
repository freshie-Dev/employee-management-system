using EMS.Models;
using Microsoft.EntityFrameworkCore;

namespace EMS.DB;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeAssignment> EmployeeAssignments => Set<EmployeeAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(e => {
            e.ToTable("Departments","dbo"); e.HasKey(x=>x.DepartmentId);
            e.Property(x=>x.Name).HasMaxLength(100).IsRequired(); e.Property(x=>x.Code).IsUnicode(false).HasMaxLength(20).IsRequired();
            e.HasIndex(x=>x.Code).IsUnique(); e.HasOne<Department>().WithMany().HasForeignKey(x=>x.ParentDepartmentId).OnDelete(DeleteBehavior.NoAction);
            e.Property(x=>x.IsActive).HasDefaultValue(true); e.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        });
        modelBuilder.Entity<Designation>(e => {
            e.ToTable("Designations","dbo"); e.HasKey(x=>x.DesignationId);
            e.Property(x=>x.Title).HasMaxLength(100).IsRequired(); e.Property(x=>x.Code).IsUnicode(false).HasMaxLength(20).IsRequired();
            e.Property(x=>x.Description).HasMaxLength(500); e.HasIndex(x=>x.Code).IsUnique();
            e.Property(x=>x.IsActive).HasDefaultValue(true); e.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        });
        modelBuilder.Entity<Employee>(e => {
            e.ToTable("Employees","dbo"); e.HasKey(x=>x.EmployeeId);
            e.Property(x=>x.EmployeeCode).IsUnicode(false).HasMaxLength(30).IsRequired(); e.HasIndex(x=>x.EmployeeCode).IsUnique();
            e.Property(x=>x.FirstName).HasMaxLength(100).IsRequired(); e.Property(x=>x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x=>x.WorkEmail).HasMaxLength(255).IsRequired(); e.HasIndex(x=>x.WorkEmail).IsUnique();
            e.Property(x=>x.PersonalEmail).HasMaxLength(255); e.Property(x=>x.PhoneNumber).IsUnicode(false).HasMaxLength(30);
            e.Property(x=>x.HireDate).HasColumnType("date"); e.Property(x=>x.DateOfBirth).HasColumnType("date");
            e.Property(x=>x.EmploymentType).IsUnicode(false).HasMaxLength(20); e.Property(x=>x.EmploymentStatus).IsUnicode(false).HasMaxLength(20).HasDefaultValue("ACTIVE");
            e.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()"); e.Property(x=>x.UpdatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        });
        modelBuilder.Entity<EmployeeAssignment>(e => {
            e.ToTable("EmployeeAssignments","dbo"); e.HasKey(x=>x.AssignmentId);
            e.HasOne<Employee>().WithMany().HasForeignKey(x=>x.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Department>().WithMany().HasForeignKey(x=>x.DepartmentId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Designation>().WithMany().HasForeignKey(x=>x.DesignationId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Employee>().WithMany().HasForeignKey(x=>x.ManagerId).OnDelete(DeleteBehavior.NoAction);
            e.Property(x=>x.EffectiveFrom).HasColumnType("date"); e.Property(x=>x.EffectiveTo).HasColumnType("date");
            e.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x=>new {x.EmployeeId,x.EffectiveFrom}).IsUnique();
            e.HasIndex(x=>x.EmployeeId).IsUnique().HasFilter("[EffectiveTo] IS NULL").HasDatabaseName("UX_EA_Current");
        });
        modelBuilder.Entity<UserAccount>().HasOne<Employee>().WithOne().HasForeignKey<UserAccount>(x=>x.EmployeeId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("UserAccounts", "dbo");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.LastLoginAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.EmployeeId).IsUnique().HasFilter("[EmployeeId] IS NOT NULL");
        });
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles", "dbo");
            entity.HasKey(x => x.RoleId);
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(255);
            entity.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles", "dbo");
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
