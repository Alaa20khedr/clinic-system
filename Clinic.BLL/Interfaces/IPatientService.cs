using System.Collections.Generic;
using System.Threading.Tasks;
using Clinic.BLL.Common;
using Clinic.BLL.Models;

namespace Clinic.BLL.Interfaces;

public interface IPatientService
{
    Task<List<PatientListItemDto>> GetPatientsAsync(string? searchTerm = null);
    Task<PatientDetailsDto?> GetPatientByIdAsync(int id);
    Task<OperationResult<int>> CreatePatientAsync(CreatePatientDto dto);
    Task<OperationResult> UpdatePatientAsync(UpdatePatientDto dto);
    Task<OperationResult> DeletePatientAsync(int id);
}

