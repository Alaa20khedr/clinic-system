using System;
using System.ComponentModel.DataAnnotations;

namespace Clinic.PL.ViewModels;

public class DoctorScheduleVM
{
    public int Id { get; set; }

    [Required]
    public int DoctorId { get; set; }

    [Required(ErrorMessage = "Select a day of the week")]
    [Display(Name = "Day of week")]
    public DayOfWeek DayOfWeek { get; set; }

    [Required(ErrorMessage = "Start time is required")]
    [Display(Name = "Start time")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "End time is required")]
    [Display(Name = "End time")]
    public TimeOnly EndTime { get; set; }
}

