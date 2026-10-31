using System;
using System.Collections.Generic;

namespace Clinic.DAL.Entities;

public class Patient
{
    public int Id { get; set; }

    public int ClinicId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;

    public Clinic Clinic { get; set; } = null!;

    public ICollection<Appointment> Appointments { get; set; }
        = new List<Appointment>();
}
