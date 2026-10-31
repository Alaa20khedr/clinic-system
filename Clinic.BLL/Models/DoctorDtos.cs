using System;
using System.Collections.Generic;

namespace Clinic.BLL.Models;

public record DoctorListItemDto(
    int Id,
    string Name,
    string? Specialty,
    int ScheduleCount,
    int AppointmentCount
);

public record DoctorScheduleDto(
    int Id,
    int DoctorId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime
);

public record DoctorDayScheduleDto(
    DayOfWeek DayOfWeek,
    string DayName,
    bool IsDayOff,
    List<DoctorScheduleDto> Periods
);

public record DoctorDetailsDto(
    int Id,
    string Name,
    string? Specialty,
    List<DoctorDayScheduleDto> WeeklySchedules,
    int TotalAppointmentsCount
);

public record CreateDoctorDto(
    string Name,
    string? Specialty
);

public record UpdateDoctorDto(
    int Id,
    string Name,
    string? Specialty
);

public record AddDoctorScheduleDto(
    int DoctorId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime
);

public record UpdateDoctorScheduleDto(
    int Id,
    int DoctorId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime
);

