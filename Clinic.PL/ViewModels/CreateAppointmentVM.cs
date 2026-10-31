using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Clinic.BLL.Models;
using Clinic.PL.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Clinic.PL.ViewModels;

public class CreateAppointmentVM
{
    public int? PatientId { get; set; }

    [Required(ErrorMessage = "Patient name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Patient name must be between 2 and 150 characters")]
    [RegularExpression(@"^[a-zA-Z\u0600-\u06FF\s'’\.\-]+$", ErrorMessage = "Patient name can only contain letters, spaces, hyphens, and dots")]
    [Display(Name = "Full name")]
    public string PatientFullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required")]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [EgyptianPhone]
    [Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please select a doctor")]
    [Display(Name = "Doctor")]
    public int DoctorId { get; set; }

    [Required(ErrorMessage = "Please select an appointment date")]
    [Display(Name = "Appointment date")]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Please select a time slot")]
    [Display(Name = "Time slot")]
    public TimeOnly? StartTime { get; set; }

    [ValidateNever]
    public List<DoctorOption> Doctors { get; set; } = new();
}
