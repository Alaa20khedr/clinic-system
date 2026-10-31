using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clinic.DAL.Enums;

namespace Clinic.DAL.Entities;

public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public AppointmentStatus Status { get; set; }
        = AppointmentStatus.Booked;

    public Patient Patient { get; set; } = null!;

    public Doctor Doctor { get; set; } = null!;
}

