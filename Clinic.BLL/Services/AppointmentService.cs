using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Clinic.BLL.Common;
using Clinic.BLL.Helpers;
using Clinic.BLL.Interfaces;
using Clinic.BLL.Models;
using Clinic.DAL.Data;
using Clinic.DAL.Entities;
using Clinic.DAL.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Clinic.BLL.Services;

public class AppointmentService : IAppointmentService
{
    private const int SlotMinutes = 30;
    private readonly ClinicDbContext _db;

    public AppointmentService(ClinicDbContext db) => _db = db;

    public async Task<List<DoctorOption>> GetDoctorsAsync() =>
        await _db.Doctors.AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DoctorOption(d.Id, d.Name))
            .ToListAsync();

    public async Task<List<SlotDto>> GetFreeSlotsAsync(int doctorId, DateOnly date)
    {
        var now = DateTime.Now;
        if (date < DateOnly.FromDateTime(now)) return new();

        var dow = date.DayOfWeek;
        var schedules = await _db.DoctorSchedules.AsNoTracking()
            .Where(s => s.DoctorId == doctorId && s.DayOfWeek == dow)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        if (schedules.Count == 0) return new();

        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        var booked = await _db.Appointments.AsNoTracking()
            .Where(a => a.DoctorId == doctorId
                     && a.Status != AppointmentStatus.Cancelled
                     && a.StartAt >= dayStart && a.StartAt < dayEnd)
            .Select(a => new { a.StartAt, a.EndAt })
            .ToListAsync();

        var step = TimeSpan.FromMinutes(SlotMinutes);
        var slots = new List<SlotDto>();

        foreach (var s in schedules)
        {
            var end = s.EndTime.ToTimeSpan();
            for (var t = s.StartTime.ToTimeSpan(); t + step <= end; t += step)
            {
                var slotStart = dayStart + t;
                var slotEnd = slotStart + step;

                if (slotStart <= now) continue;
                if (booked.Any(b => slotStart < b.EndAt && slotEnd > b.StartAt))
                    continue;

                slots.Add(new SlotDto(TimeOnly.FromTimeSpan(t), TimeOnly.FromTimeSpan(t + step)));
            }
        }

        return slots;
    }

    public async Task<BookingResult> BookAsync(BookAppointmentRequest r)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (r.Date < today)
            return BookingResult.Fail("Cannot book an appointment for a past date.");

        var doctor = await _db.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Id == r.DoctorId);
        if (doctor is null) return BookingResult.Fail("Doctor not found.");

        Patient patient;

        if (r.PatientId is int patientId)
        {
            // Existing patient selected
            var existing = await _db.Patients.FirstOrDefaultAsync(p => p.Id == patientId);
            if (existing is null)
                return BookingResult.Fail("Selected patient was not found.");
            if (existing.ClinicId != doctor.ClinicId)
                return BookingResult.Fail("Patient and doctor must belong to the same clinic.");

            patient = existing;
        }
        else
        {
            // New patient: validation
            var rawName = r.PatientFullName?.Trim() ?? string.Empty;
            var name = Regex.Replace(rawName, @"\s+", " ");

            if (name.Length < 2 || name.Length > 150 || !Regex.IsMatch(name, @"^[a-zA-Z\u0600-\u06FF\s'’\.\-]+$") || !name.Any(char.IsLetter))
                return BookingResult.Fail("Patient name must be 2 to 150 characters and contain valid letters.");

            if (!PhoneNumberNormalizer.TryNormalize(r.PhoneNumber, out var normPhone))
                return BookingResult.Fail("Enter a valid Egyptian mobile number (e.g. 01012345678).");

            if (r.DateOfBirth > today)
                return BookingResult.Fail("Date of birth cannot be in the future.");
            if (r.DateOfBirth < today.AddYears(-120))
                return BookingResult.Fail("Date of birth cannot be older than 120 years.");

            patient = await _db.Patients.FirstOrDefaultAsync(p =>
                    p.ClinicId == doctor.ClinicId &&
                    p.FullName == name &&
                    p.PhoneNumber == normPhone &&
                    p.DateOfBirth == r.DateOfBirth)
                ?? new Patient
                {
                    ClinicId = doctor.ClinicId,
                    FullName = name,
                    PhoneNumber = normPhone,
                    DateOfBirth = r.DateOfBirth
                };
        }

        var free = await GetFreeSlotsAsync(r.DoctorId, r.Date);
        if (!free.Any(s => s.Start == r.StartTime))
            return BookingResult.Fail("This slot is no longer available. Please choose another one.");

        var start = r.Date.ToDateTime(r.StartTime);
        var appointment = new Appointment
        {
            DoctorId = doctor.Id,
            Patient = patient,
            StartAt = start,
            EndAt = start.AddMinutes(SlotMinutes),
            Status = AppointmentStatus.Booked
        };

        _db.Appointments.Add(appointment);

        try
        {
            await _db.SaveChangesAsync();
            return BookingResult.Ok(appointment.Id);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return BookingResult.Fail("This slot was just booked by someone else. Please choose another one.");
        }
    }

    public async Task<List<AppointmentGridItem>> GetAppointmentsAsync(DateOnly? date, int? doctorId)
    {
        var query = _db.Appointments.AsNoTracking().AsQueryable();

        if (date is { } d)
        {
            var from = d.ToDateTime(TimeOnly.MinValue);
            var to = from.AddDays(1);
            query = query.Where(a => a.StartAt >= from && a.StartAt < to);
        }

        if (doctorId is { } id)
        {
            query = query.Where(a => a.DoctorId == id);
        }

        var rows = await query
            .OrderBy(a => a.StartAt)
            .Select(a => new
            {
                a.Id,
                PatientName = a.Patient.FullName,
                DoctorName = a.Doctor.Name,
                a.StartAt,
                a.EndAt,
                a.Status
            })
            .ToListAsync();

        return rows.Select(x => new AppointmentGridItem(
            x.Id,
            x.PatientName,
            x.DoctorName,
            DateOnly.FromDateTime(x.StartAt),
            TimeOnly.FromDateTime(x.StartAt),
            TimeOnly.FromDateTime(x.EndAt),
            x.Status.ToString())).ToList();
    }

    public async Task<OperationResult> CancelAsync(int id)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null)
        {
            return OperationResult.Fail("Appointment not found.");
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return OperationResult.Fail("Only booked appointments can be cancelled.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();

        return OperationResult.Ok();
    }

    public async Task<OperationResult> CompleteAsync(int id)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null)
        {
            return OperationResult.Fail("Appointment not found.");
        }

        if (appointment.Status != AppointmentStatus.Booked)
        {
            return OperationResult.Fail("Only booked appointments can be marked as completed.");
        }

        if (appointment.StartAt > DateTime.Now)
        {
            return OperationResult.Fail("Cannot complete an appointment that has not started yet.");
        }

        appointment.Status = AppointmentStatus.Completed;
        await _db.SaveChangesAsync();

        return OperationResult.Ok();
    }

    public async Task<AppointmentDetailsDto?> GetAppointmentDetailsAsync(int id)
    {
        var appointment = await _db.Appointments.AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment is null) return null;

        return new AppointmentDetailsDto(
            appointment.Id,
            appointment.PatientId,
            appointment.Patient.FullName,
            appointment.Patient.PhoneNumber,
            appointment.Patient.DateOfBirth,
            appointment.DoctorId,
            appointment.Doctor.Name,
            appointment.Doctor.Specialty,
            DateOnly.FromDateTime(appointment.StartAt),
            TimeOnly.FromDateTime(appointment.StartAt),
            TimeOnly.FromDateTime(appointment.EndAt),
            appointment.Status.ToString()
        );
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);
}
