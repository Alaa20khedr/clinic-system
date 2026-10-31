using Clinic.BLL.Interfaces;
using Clinic.BLL.Services;
using Clinic.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clinic.BLL;

public static class DependencyInjection
{
    public static IServiceCollection AddClinicServices(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ClinicDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDoctorService, DoctorService>();
        return services;
    }
}
