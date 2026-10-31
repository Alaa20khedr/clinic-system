using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Clinic.DAL.Entities
{
    public class Clinic
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public ICollection<Doctor> Doctors { get; set; }
            = new List<Doctor>();

        public ICollection<Patient> Patients { get; set; }
            = new List<Patient>();
    }
}
