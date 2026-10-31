using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Clinic.BLL.Models;

namespace Clinic.PL.ViewModels;

public class DoctorListItemVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public int ScheduleCount { get; set; }
    public int AppointmentCount { get; set; }
}

public class DoctorCreateEditVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Doctor name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Doctor name must be between 2 and 150 characters")]
    [Display(Name = "Doctor name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Specialty cannot exceed 100 characters")]
    [Display(Name = "Specialty")]
    public string? Specialty { get; set; }
}

public class DoctorDetailsVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public int TotalAppointmentsCount { get; set; }
    public List<DoctorDayScheduleDto> WeeklySchedules { get; set; } = new();
    public DoctorScheduleVM ScheduleForm { get; set; } = new();
}

