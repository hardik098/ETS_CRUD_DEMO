using ETS_CRUD_DEMO.Models;
using Microsoft.EntityFrameworkCore;

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
            modelBuilder.Entity<Employee>()
                .Property(e => e.Gender)
                .HasConversion<string>(); // Store gender as a string in the database

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId);

            modelBuilder.Entity<City>()
                .HasOne(c => c.State)
                .WithMany()
                .HasForeignKey(c => c.StateId);
        }
    }
}
