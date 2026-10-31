using System;
using System.Collections.Generic;
using System.Linq;
using Clinic.DAL.Entities;
using Clinic.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using ClinicEntity = Clinic.DAL.Entities.Clinic;

namespace Clinic.DAL.Data;

public class ClinicDbContext : DbContext
{
    public ClinicDbContext(DbContextOptions<ClinicDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClinicEntity> Clinics => Set<ClinicEntity>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Clinic
        modelBuilder.Entity<ClinicEntity>(entity =>
        {
            entity.ToTable("Clinics");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(150);
        });

        // Doctor
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctors");
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(d => d.Specialty)
                .HasMaxLength(100);

            entity.HasOne(d => d.Clinic)
                .WithMany(c => c.Doctors)
                .HasForeignKey(d => d.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(d => d.ClinicId);
        });

        // Doctor Schedule
        modelBuilder.Entity<DoctorSchedule>(entity =>
        {
            entity.ToTable("DoctorSchedules", table =>
                table.HasCheckConstraint(
                    "CK_DoctorSchedules_ValidTime",
                    "[EndTime] > [StartTime]"));

            entity.HasKey(s => s.Id);

            entity.Property(s => s.StartTime).HasColumnType("time");
            entity.Property(s => s.EndTime).HasColumnType("time");

            entity.HasOne(s => s.Doctor)
                .WithMany(d => d.Schedules)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(s => new { s.DoctorId, s.DayOfWeek, s.StartTime })
                .IsUnique();
        });

        // Patient
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.FullName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.DateOfBirth)
                .HasColumnType("date")
                .IsRequired();

            entity.Property(p => p.PhoneNumber)
                .HasMaxLength(11)
                .IsRequired();

            entity.HasOne(p => p.Clinic)
                .WithMany(c => c.Patients)
                .HasForeignKey(p => p.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.ClinicId);
        });

        // Appointment
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments", table =>
                table.HasCheckConstraint(
                    "CK_Appointments_ValidTime",
                    "[EndAt] > [StartAt]"));

            entity.HasKey(a => a.Id);

            entity.Property(a => a.Status).HasConversion<int>();

            entity.HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => new { a.DoctorId, a.StartAt })
                .IsUnique()
                .HasFilter("[Status] <> 2");

            entity.HasIndex(a => a.PatientId);
        });

        // Seed Clinic
        modelBuilder.Entity<ClinicEntity>().HasData(
            new ClinicEntity { Id = 1, Name = "My Clinic" });

        // Seed Doctors: Dr. Ahmed (General) and Dr. Sara (Pediatrics)
        modelBuilder.Entity<Doctor>().HasData(
            new Doctor { Id = 1, ClinicId = 1, Name = "Dr. Ahmed", Specialty = "General" },
            new Doctor { Id = 2, ClinicId = 1, Name = "Dr. Sara", Specialty = "Pediatrics" });

        // Dr. Ahmed Schedules: Saturday to Thursday, 16:00 to 20:00 (No Friday)
        var drAhmedWorkDays = new[]
        {
            DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday,
            DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday
        };

        var schedules = drAhmedWorkDays.Select((day, i) => new DoctorSchedule
        {
            Id = i + 1,
            DoctorId = 1,
            DayOfWeek = day,
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(20, 0)
        }).ToList();

        // Dr. Sara Schedules: Sunday, Tuesday, Thursday from 10:00 to 14:00
        schedules.Add(new DoctorSchedule
        {
            Id = 7,
            DoctorId = 2,
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(14, 0)
        });
        schedules.Add(new DoctorSchedule
        {
            Id = 8,
            DoctorId = 2,
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(14, 0)
        });
        schedules.Add(new DoctorSchedule
        {
            Id = 9,
            DoctorId = 2,
            DayOfWeek = DayOfWeek.Thursday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(14, 0)
        });

        modelBuilder.Entity<DoctorSchedule>().HasData(schedules);
    }
}
