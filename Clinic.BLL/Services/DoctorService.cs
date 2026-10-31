using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Clinic.BLL.Common;
using Clinic.BLL.Interfaces;
using Clinic.BLL.Models;
using Clinic.DAL.Data;
using Clinic.DAL.Entities;
using Clinic.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace Clinic.BLL.Services;

public class DoctorService : IDoctorService
{
    private readonly ClinicDbContext _db;

    public DoctorService(ClinicDbContext db) => _db = db;

    public async Task<List<DoctorListItemDto>> GetDoctorsAsync()
    {
        return await _db.Doctors.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DoctorListItemDto(
                d.Id,
                d.Name,
                d.Specialty,
                d.Schedules.Count,
                d.Appointments.Count
            ))
            .ToListAsync();
    }

    public async Task<DoctorDetailsDto?> GetDoctorByIdAsync(int id)
    {
        // من غير Include للـ Appointments: بنعدّهم بـ CountAsync بدل ما نحمّل تاريخ الدكتور كله
        var doctor = await _db.Doctors.AsNoTracking()
            .Include(d => d.Schedules)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doctor is null) return null;

        var appointmentCount = await _db.Appointments.CountAsync(a => a.DoctorId == id);

        var daysOrder = new[]
        {
            DayOfWeek.Saturday,
            DayOfWeek.Sunday,
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        };

        var weekly = new List<DoctorDayScheduleDto>();
        foreach (var dow in daysOrder)
        {
            var periods = doctor.Schedules
                .Where(s => s.DayOfWeek == dow)
                .OrderBy(s => s.StartTime)
                .Select(s => new DoctorScheduleDto(s.Id, s.DoctorId, s.DayOfWeek, s.StartTime, s.EndTime))
                .ToList();

            weekly.Add(new DoctorDayScheduleDto(
                dow,
                dow.ToString(),
                periods.Count == 0,
                periods
            ));
        }

        return new DoctorDetailsDto(
            doctor.Id,
            doctor.Name,
            doctor.Specialty,
            weekly,
            appointmentCount
        );
    }

    public async Task<OperationResult<int>> CreateDoctorAsync(CreateDoctorDto dto)
    {
        var rawName = dto.Name?.Trim() ?? string.Empty;
        var name = Regex.Replace(rawName, @"\s+", " ");

        if (name.Length < 2 || name.Length > 150)
        {
            return OperationResult<int>.Fail("Doctor name must be between 2 and 150 characters.");
        }

        var specialty = string.IsNullOrWhiteSpace(dto.Specialty) ? null : dto.Specialty.Trim();
        if (specialty is not null && specialty.Length > 100)
        {
            return OperationResult<int>.Fail("Specialty cannot exceed 100 characters.");
        }

        // OrderBy عشان النتيجة تبقى ثابتة (أول عيادة بالـ Id) مش عشوائية
        var clinicId = await _db.Clinics
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        if (clinicId == 0)
        {
            return OperationResult<int>.Fail("Default clinic could not be resolved.");
        }

        var doctor = new Doctor
        {
            ClinicId = clinicId,
            Name = name,
            Specialty = specialty
        };

        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();

        return OperationResult<int>.Ok(doctor.Id);
    }

    public async Task<OperationResult> UpdateDoctorAsync(UpdateDoctorDto dto)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == dto.Id);
        if (doctor is null)
        {
            return OperationResult.Fail("Doctor not found.");
        }

        var rawName = dto.Name?.Trim() ?? string.Empty;
        var name = Regex.Replace(rawName, @"\s+", " ");

        if (name.Length < 2 || name.Length > 150)
        {
            return OperationResult.Fail("Doctor name must be between 2 and 150 characters.");
        }

        var specialty = string.IsNullOrWhiteSpace(dto.Specialty) ? null : dto.Specialty.Trim();
        if (specialty is not null && specialty.Length > 100)
        {
            return OperationResult.Fail("Specialty cannot exceed 100 characters.");
        }

        doctor.Name = name;
        doctor.Specialty = specialty;

        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteDoctorAsync(int id)
    {
        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == id);
        if (doctor is null)
        {
            return OperationResult.Fail("Doctor not found.");
        }

        var hasAppointments = await _db.Appointments.AnyAsync(a => a.DoctorId == id);
        if (hasAppointments)
        {
            return OperationResult.Fail("Cannot delete: doctor has appointments.");
        }

        // نمسح صفوف الجدول الأول (الـ FKs بـ Restrict)
        var schedules = await _db.DoctorSchedules.Where(s => s.DoctorId == id).ToListAsync();
        if (schedules.Count > 0)
        {
            _db.DoctorSchedules.RemoveRange(schedules);
        }

        _db.Doctors.Remove(doctor);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // حد حجز للدكتور في نفس اللحظة، والـ FK (Restrict) رفض الحذف
            return OperationResult.Fail("Cannot delete: doctor has appointments.");
        }

        return OperationResult.Ok();
    }

    public async Task<OperationResult<int>> AddScheduleAsync(AddDoctorScheduleDto dto)
    {
        var doctorExists = await _db.Doctors.AnyAsync(d => d.Id == dto.DoctorId);
        if (!doctorExists)
        {
            return OperationResult<int>.Fail("Doctor not found.");
        }

        if (dto.EndTime <= dto.StartTime)
        {
            return OperationResult<int>.Fail("End time must be after start time.");
        }

        if (dto.StartTime.Minute % 30 != 0 || dto.EndTime.Minute % 30 != 0 || dto.StartTime.Second != 0 || dto.EndTime.Second != 0)
        {
            return OperationResult<int>.Fail("Working hours must align with 30-minute intervals (e.g. 09:00, 09:30).");
        }

        var existingSchedules = await _db.DoctorSchedules.AsNoTracking()
            .Where(s => s.DoctorId == dto.DoctorId && s.DayOfWeek == dto.DayOfWeek)
            .ToListAsync();

        if (existingSchedules.Any(s => s.StartTime == dto.StartTime))
        {
            return OperationResult<int>.Fail("A shift starting at this time already exists on this day.");
        }

        if (existingSchedules.Any(s => s.StartTime < dto.EndTime && s.EndTime > dto.StartTime))
        {
            return OperationResult<int>.Fail("This schedule overlaps with an existing shift on this day.");
        }

        var schedule = new DoctorSchedule
        {
            DoctorId = dto.DoctorId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime
        };

        _db.DoctorSchedules.Add(schedule);
        await _db.SaveChangesAsync();

        return OperationResult<int>.Ok(schedule.Id);
    }

    public async Task<OperationResult> UpdateScheduleAsync(UpdateDoctorScheduleDto dto)
    {
        var schedule = await _db.DoctorSchedules.FirstOrDefaultAsync(s => s.Id == dto.Id);
        if (schedule is null)
        {
            return OperationResult.Fail("Schedule shift not found.");
        }

        if (dto.EndTime <= dto.StartTime)
        {
            return OperationResult.Fail("End time must be after start time.");
        }

        if (dto.StartTime.Minute % 30 != 0 || dto.EndTime.Minute % 30 != 0 || dto.StartTime.Second != 0 || dto.EndTime.Second != 0)
        {
            return OperationResult.Fail("Working hours must align with 30-minute intervals (e.g. 09:00, 09:30).");
        }

        // نستخدم دكتور الصف نفسه (schedule.DoctorId) مش اللي جاي من الفورم
        var otherSchedules = await _db.DoctorSchedules.AsNoTracking()
            .Where(s => s.DoctorId == schedule.DoctorId && s.DayOfWeek == dto.DayOfWeek && s.Id != dto.Id)
            .ToListAsync();

        if (otherSchedules.Any(s => s.StartTime == dto.StartTime))
        {
            return OperationResult.Fail("A shift starting at this time already exists on this day.");
        }

        if (otherSchedules.Any(s => s.StartTime < dto.EndTime && s.EndTime > dto.StartTime))
        {
            return OperationResult.Fail("This schedule overlaps with an existing shift on this day.");
        }

        // نرفض التعديل بس لو في حجز مستقبلي جوه الفترة الحالية هيطلع برّا الفترة الجديدة
        var futureAppointments = await _db.Appointments.AsNoTracking()
            .Where(a => a.DoctorId == schedule.DoctorId
                     && a.Status != AppointmentStatus.Cancelled
                     && a.StartAt >= DateTime.Now)
            .ToListAsync();

        var wouldBeUncovered = futureAppointments.Any(a =>
        {
            var start = TimeOnly.FromDateTime(a.StartAt);
            var end = TimeOnly.FromDateTime(a.EndAt);

            var insideCurrentShift =
                a.StartAt.DayOfWeek == schedule.DayOfWeek &&
                start >= schedule.StartTime &&
                end <= schedule.EndTime;

            if (!insideCurrentShift) return false;

            var stillCoveredAfterEdit =
                a.StartAt.DayOfWeek == dto.DayOfWeek &&
                start >= dto.StartTime &&
                end <= dto.EndTime;

            return !stillCoveredAfterEdit;
        });

        if (wouldBeUncovered)
        {
            return OperationResult.Fail("Cannot modify this period because future appointments would fall outside the new working hours.");
        }

        schedule.DayOfWeek = dto.DayOfWeek;
        schedule.StartTime = dto.StartTime;
        schedule.EndTime = dto.EndTime;

        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeleteScheduleAsync(int scheduleId)
    {
        var schedule = await _db.DoctorSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule is null)
        {
            return OperationResult.Fail("Schedule shift not found.");
        }

        var futureAppointments = await _db.Appointments.AsNoTracking()
            .Where(a => a.DoctorId == schedule.DoctorId
                     && a.Status != AppointmentStatus.Cancelled
                     && a.StartAt >= DateTime.Now)
            .ToListAsync();

        var hasFutureInShift = futureAppointments.Any(a =>
            a.StartAt.DayOfWeek == schedule.DayOfWeek &&
            TimeOnly.FromDateTime(a.StartAt) >= schedule.StartTime &&
            TimeOnly.FromDateTime(a.EndAt) <= schedule.EndTime);

        if (hasFutureInShift)
        {
            return OperationResult.Fail("Cannot delete schedule period because there are future appointments booked within this working shift.");
        }

        _db.DoctorSchedules.Remove(schedule);
        await _db.SaveChangesAsync();

        return OperationResult.Ok();
    }
}