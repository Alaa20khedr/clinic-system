using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic.BLL.Models
{
    public class BookingResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public int? AppointmentId { get; init; }

        public static BookingResult Ok(int id) => new() { Success = true, AppointmentId = id };
        public static BookingResult Fail(string error) => new() { Error = error };
    }
}
