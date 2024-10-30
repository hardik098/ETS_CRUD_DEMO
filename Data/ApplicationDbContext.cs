using ETS_CRUD_DEMO.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ETS_CRUD_DEMO.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure GUID primary keys
            modelBuilder.Entity<Employee>()
                .Property(e => e.EmployeeId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Department>()
                .Property(d => d.DepartmentId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Role>()
                .Property(r => r.RoleId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<State>()
                .Property(s => s.StateId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<City>()
                .Property(c => c.CityId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Employee>()
                .Property(e => e.Gender)
                .HasConversion<string>(); // Store gender as a string in the database

            // Configure relationships
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Role)
                .WithMany(r => r.Employees)
                .HasForeignKey(e => e.RoleId);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.State)
                .WithMany(s => s.Employees)
                .HasForeignKey(e => e.StateId);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.City)
                .WithMany(c => c.Employees)
                .HasForeignKey(e => e.CityId);

            modelBuilder.Entity<City>()
                .HasOne(c => c.State)
                .WithMany(s => s.Cities)
                .HasForeignKey(c => c.StateId);

            // Configure Skills to be stored as JSON
            modelBuilder.Entity<Employee>()
                .Property(e => e.Skills)
                .HasConversion(
                    skills => JsonSerializer.Serialize(skills, (JsonSerializerOptions)null),
                    skills => JsonSerializer.Deserialize<List<string>>(skills, (JsonSerializerOptions)null)
                );
        }
    }
}
