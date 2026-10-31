using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Clinic.BLL.Models;
using Clinic.PL.Validation;

namespace Clinic.PL.ViewModels;

public class PatientListItemVM
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
}

public class PatientListVM
{
    public List<PatientListItemVM> Patients { get; set; } = new();
    public string? Search { get; set; }
}

public class PatientCreateEditVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters")]
    [RegularExpression(@"^[a-zA-Z\u0600-\u06FF\s'’\.\-]+$", ErrorMessage = "Full name can only contain letters, spaces, hyphens, and dots")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required")]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [EgyptianPhone]
    [Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class PatientDetailsVM
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public List<AppointmentGridItem> Appointments { get; set; } = new();
}
