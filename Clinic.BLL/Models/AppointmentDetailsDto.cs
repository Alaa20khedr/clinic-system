using System;

namespace Clinic.BLL.Models;

public record AppointmentDetailsDto(
    int Id,
    int PatientId,
    string PatientFullName,
    string PatientPhoneNumber,
    DateOnly PatientDateOfBirth,
    int DoctorId,
    string DoctorName,
    string? DoctorSpecialty,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status
);

