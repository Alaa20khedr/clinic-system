using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic.DAL.Entities
{
    public class Doctor
    {
        public int Id { get; set; }

        public int ClinicId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Specialty { get; set; }

        public Clinic Clinic { get; set; } = null!;

        public ICollection<DoctorSchedule> Schedules { get; set; }
            = new List<DoctorSchedule>();

        public ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();
    }
}
