
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
using Microsoft.EntityFrameworkCore;

namespace Clinic.BLL.Services;

public class PatientService : IPatientService
{
    private readonly ClinicDbContext _db;

    public PatientService(ClinicDbContext db) => _db = db;

    public async Task<List<PatientListItemDto>> GetPatientsAsync(string? searchTerm = null)
    {
        var query = _db.Patients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();

            var phoneTerm = PhoneNumberNormalizer.TryNormalize(term, out var normalized)
                ? normalized
                : term;

            query = query.Where(p => p.FullName.Contains(term) || p.PhoneNumber.Contains(phoneTerm));
        }

        return await query
            .OrderBy(p => p.FullName)
            .Select(p => new PatientListItemDto(
                p.Id,
                p.FullName,
                p.DateOfBirth,
                p.PhoneNumber,
                p.Appointments.Count
            ))
            .ToListAsync();
    }

    public async Task<PatientDetailsDto?> GetPatientByIdAsync(int id)
    {
        var patient = await _db.Patients.AsNoTracking()
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Doctor)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient is null) return null;

        var appointments = patient.Appointments
            .OrderByDescending(a => a.StartAt)
            .Select(a => new AppointmentGridItem(
                a.Id,
                patient.FullName,
                a.Doctor.Name,
                DateOnly.FromDateTime(a.StartAt),
                TimeOnly.FromDateTime(a.StartAt),
                TimeOnly.FromDateTime(a.EndAt),
                a.Status.ToString()
            ))
            .ToList();

        return new PatientDetailsDto(
            patient.Id,
            patient.FullName,
            patient.DateOfBirth,
            patient.PhoneNumber,
            appointments
        );
    }

    public async Task<OperationResult<int>> CreatePatientAsync(CreatePatientDto dto)
    {
        var rawName = dto.FullName?.Trim() ?? string.Empty;
        var name = Regex.Replace(rawName, @"\s+", " ");

        if (name.Length < 2 || name.Length > 150 || !Regex.IsMatch(name, @"^[a-zA-Z\u0600-\u06FF\s'’\.\-]+$") || !name.Any(char.IsLetter))
        {
            return OperationResult<int>.Fail("Patient name must be 2 to 150 characters and contain valid letters.");
        }

        if (!PhoneNumberNormalizer.TryNormalize(dto.PhoneNumber, out var normPhone))
        {
            return OperationResult<int>.Fail("Enter a valid Egyptian mobile number (e.g. 01012345678).");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (dto.DateOfBirth > today)
        {
            return OperationResult<int>.Fail("Date of birth cannot be in the future.");
        }
        if (dto.DateOfBirth < today.AddYears(-120))
        {
            return OperationResult<int>.Fail("Date of birth cannot be older than 120 years.");
        }

        var clinicId = await _db.Clinics
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        if (clinicId == 0)
        {
            return OperationResult<int>.Fail("Default clinic could not be resolved.");
        }

        var duplicateExists = await _db.Patients.AnyAsync(p =>
            p.ClinicId == clinicId &&
            p.FullName == name &&
            p.PhoneNumber == normPhone &&
            p.DateOfBirth == dto.DateOfBirth);

        if (duplicateExists)
        {
            return OperationResult<int>.Fail("A patient with the same name, date of birth, and phone number already exists.");
        }

        var patient = new Patient
        {
            ClinicId = clinicId,
            FullName = name,
            PhoneNumber = normPhone,
            DateOfBirth = dto.DateOfBirth
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        return OperationResult<int>.Ok(patient.Id);
    }

    public async Task<OperationResult> UpdatePatientAsync(UpdatePatientDto dto)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == dto.Id);
        if (patient is null)
        {
            return OperationResult.Fail("Patient not found.");
        }

        var rawName = dto.FullName?.Trim() ?? string.Empty;
        var name = Regex.Replace(rawName, @"\s+", " ");

        if (name.Length < 2 || name.Length > 150 || !Regex.IsMatch(name, @"^[a-zA-Z\u0600-\u06FF\s'’\.\-]+$") || !name.Any(char.IsLetter))
        {
            return OperationResult.Fail("Patient name must be 2 to 150 characters and contain valid letters.");
        }

        if (!PhoneNumberNormalizer.TryNormalize(dto.PhoneNumber, out var normPhone))
        {
            return OperationResult.Fail("Enter a valid Egyptian mobile number (e.g. 01012345678).");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (dto.DateOfBirth > today)
        {
            return OperationResult.Fail("Date of birth cannot be in the future.");
        }
        if (dto.DateOfBirth < today.AddYears(-120))
        {
            return OperationResult.Fail("Date of birth cannot be older than 120 years.");
        }

        var duplicateExists = await _db.Patients.AnyAsync(p =>
            p.ClinicId == patient.ClinicId &&
            p.Id != dto.Id &&
            p.FullName == name &&
            p.PhoneNumber == normPhone &&
            p.DateOfBirth == dto.DateOfBirth);

        if (duplicateExists)
        {
            return OperationResult.Fail("A patient with the same name, date of birth, and phone number already exists.");
        }

        patient.FullName = name;
        patient.PhoneNumber = normPhone;
        patient.DateOfBirth = dto.DateOfBirth;

        await _db.SaveChangesAsync();
        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeletePatientAsync(int id)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (patient is null)
        {
            return OperationResult.Fail("Patient not found.");
        }

        var hasAppointments = await _db.Appointments.AnyAsync(a => a.PatientId == id);
        if (hasAppointments)
        {
            return OperationResult.Fail("Cannot delete: patient has appointments.");
        }

        _db.Patients.Remove(patient);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return OperationResult.Fail("Cannot delete: patient has appointments.");
        }

        return OperationResult.Ok();
    }
}
