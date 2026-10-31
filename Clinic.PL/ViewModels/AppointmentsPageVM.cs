using System;
using System.Collections.Generic;
using Clinic.BLL.Models;

namespace Clinic.PL.ViewModels;

public class AppointmentsPageVM
{
    public CreateAppointmentVM Form { get; set; } = new();
    public List<DoctorOption> Doctors { get; set; } = new();
    public List<AppointmentGridItem> Appointments { get; set; } = new();
    public DateOnly? FilterDate { get; set; }
    public int? FilterDoctorId { get; set; }
}
