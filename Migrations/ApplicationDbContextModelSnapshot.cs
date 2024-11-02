﻿// <auto-generated />
using System;
using ETS_CRUD_DEMO.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ETS_CRUD_DEMO.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ETS_CRUD_DEMO.Models.City", b =>
                {
                    b.Property<Guid>("CityId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("CityName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<Guid>("StateId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("CityId");

                    b.HasIndex("StateId");

                    b.ToTable("Cities");
                });

            modelBuilder.Entity("ETS_CRUD_DEMO.Models.Department", b =>
                {
                    b.Property<Guid>("DepartmentId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("DepartmentName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.HasKey("DepartmentId");

                    b.ToTable("Departments");
                });

            modelBuilder.Entity("ETS_CRUD_DEMO.Models.Employee", b =>
                {
                    b.Property<Guid>("EmployeeId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("CityId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("DOB")
                        .HasColumnType("datetime2");

                    b.Property<Guid>("DepartmentId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Gender")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsActive")
                        .HasColumnType("bit");

                    b.Property<DateTime>("JoiningDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("PhoneNumber")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProfilePicture")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Skills")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid?>("StateId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("EmployeeId");

                    b.HasIndex("CityId");

                    b.HasIndex("DepartmentId");

                    b.HasIndex("RoleId");

                    b.HasIndex("StateId");

                    b.ToTable("Employees");
                });

            modelBuilder.Entity("ETS_CRUD_DEMO.Models.Role", b =>
                {
                    b.Property<Guid>("RoleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Permissions")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("RoleName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.HasKey("RoleId");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("ETS_CRUD_DEMO.Models.State", b =>
                {
                    b.Property<Guid>("StateId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("StateName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("StateId");

                    b.ToTable("States");
                });

            modelBuilder.Entity("ETS_CRUD_DEMO.Models.City", b =>
                {
                    b.HasOne("ETS_CRUD_DEMO.Models.State", "State")
                        .WithMany("Cities")
                        .HasForeignKey("StateId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("State");
                });

            modelBuilder.Entity("ETS_CRUD_DEMO.Models.Employee", b =>
                {
                    b.HasOne("ETS_CRUD_DEMO.Models.City", "City")
                        .WithMany("Employees")
                        .HasForeignKey("CityId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ETS_CRUD_DEMO.Models.Department", "Department")
                        .WithMany("Employees")
                        .HasForeignKey("DepartmentId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ETS_CRUD_DEMO.Models.Role", "Role")
                        .WithMany("Employees")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ETS_CRUD_DEMO.Models.State", "State")
                        .WithMany("Employees")
                        .HasForeignKey("StateId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("City");

                    b.Navigation("Department");

                    b.Navigation("Role");

                    b.Navigation("State");
                });

            modelBuilder.Entity("ETS_CRUD_DEMO.Models.City", b =>
                {
                    b.Navigation("Employees");
                });

            modelBuilder.Entity("ETS_CRUD_DEMO.Models.Department", b =>
                {
                    b.Navigation("Employees");
                });

            modelBuilder.Entity("ETS_CRUD_DEMO.Models.Role", b =>
                {
                    b.Navigation("Employees");
                });

            modelBuilder.Entity("ETS_CRUD_DEMO.Models.State", b =>
                {
                    b.Navigation("Cities");

                    b.Navigation("Employees");
                });
#pragma warning restore 612, 618
        }
    }
}
