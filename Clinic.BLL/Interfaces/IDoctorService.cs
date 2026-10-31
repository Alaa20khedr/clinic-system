using System.Collections.Generic;
using System.Threading.Tasks;
using Clinic.BLL.Common;
using Clinic.BLL.Models;

namespace Clinic.BLL.Interfaces;

public interface IDoctorService
{
    Task<List<DoctorListItemDto>> GetDoctorsAsync();
    Task<DoctorDetailsDto?> GetDoctorByIdAsync(int id);
    Task<OperationResult<int>> CreateDoctorAsync(CreateDoctorDto dto);
    Task<OperationResult> UpdateDoctorAsync(UpdateDoctorDto dto);
    Task<OperationResult> DeleteDoctorAsync(int id);

    Task<OperationResult<int>> AddScheduleAsync(AddDoctorScheduleDto dto);
    Task<OperationResult> UpdateScheduleAsync(UpdateDoctorScheduleDto dto);
    Task<OperationResult> DeleteScheduleAsync(int scheduleId);
}

