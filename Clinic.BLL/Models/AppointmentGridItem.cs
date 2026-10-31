using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic.BLL.Models
{
    public record AppointmentGridItem(
      int Id, string PatientName, string DoctorName,
      DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, string Status);
}
