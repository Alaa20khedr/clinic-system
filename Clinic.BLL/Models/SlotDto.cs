using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic.BLL.Models
{
    public record SlotDto(TimeOnly Start, TimeOnly End)
    {
        public string Value => Start.ToString("HH:mm");   
        public string Label => Start.ToString("hh:mm tt", CultureInfo.InvariantCulture)
                             + " - " + End.ToString("hh:mm tt", CultureInfo.InvariantCulture);
    }
}
