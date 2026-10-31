using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Clinic.BLL.Common;
using Clinic.BLL.Models;

namespace Clinic.BLL.Interfaces;

public interface IAppointmentService
{
    Task<List<DoctorOption>> GetDoctorsAsync();
    Task<List<SlotDto>> GetFreeSlotsAsync(int doctorId, DateOnly date);
    Task<BookingResult> BookAsync(BookAppointmentRequest request);
    Task<List<AppointmentGridItem>> GetAppointmentsAsync(DateOnly? date, int? doctorId);
    Task<OperationResult> CancelAsync(int id);
    Task<OperationResult> CompleteAsync(int id);
    Task<AppointmentDetailsDto?> GetAppointmentDetailsAsync(int id);
}
