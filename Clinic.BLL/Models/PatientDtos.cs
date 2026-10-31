using System;
using System.Collections.Generic;

namespace Clinic.BLL.Models;

public record PatientListItemDto(
    int Id,
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    int AppointmentCount
);

public record PatientDetailsDto(
    int Id,
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber,
    List<AppointmentGridItem> Appointments
);

public record CreatePatientDto(
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber
);

public record UpdatePatientDto(
    int Id,
    string FullName,
    DateOnly DateOfBirth,
    string PhoneNumber
);

