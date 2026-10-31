using System.Linq;
using System.Threading.Tasks;
using Clinic.BLL.Interfaces;
using Clinic.BLL.Models;
using Clinic.PL.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.PL.Controllers;

public class DoctorsController : Controller
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = await _doctorService.GetDoctorsAsync();
        var vm = items.Select(d => new DoctorListItemVM
        {
            Id = d.Id,
            Name = d.Name,
            Specialty = d.Specialty,
            ScheduleCount = d.ScheduleCount,
            AppointmentCount = d.AppointmentCount
        }).ToList();

        return View(vm);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new DoctorCreateEditVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DoctorCreateEditVM model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _doctorService.CreateDoctorAsync(new CreateDoctorDto(
            model.Name,
            model.Specialty
        ));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not create doctor.");
            return View(model);
        }

        TempData["Success"] = "Doctor created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var doctor = await _doctorService.GetDoctorByIdAsync(id);
        if (doctor is null)
        {
            return NotFound();
        }

        var vm = new DoctorCreateEditVM
        {
            Id = doctor.Id,
            Name = doctor.Name,
            Specialty = doctor.Specialty
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DoctorCreateEditVM model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _doctorService.UpdateDoctorAsync(new UpdateDoctorDto(
            model.Id,
            model.Name,
            model.Specialty
        ));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update doctor.");
            return View(model);
        }

        TempData["Success"] = "Doctor updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var doctor = await _doctorService.GetDoctorByIdAsync(id);
        if (doctor is null)
        {
            return NotFound();
        }

        var vm = new DoctorDetailsVM
        {
            Id = doctor.Id,
            Name = doctor.Name,
            Specialty = doctor.Specialty,
            TotalAppointmentsCount = doctor.TotalAppointmentsCount,
            WeeklySchedules = doctor.WeeklySchedules,
            ScheduleForm = new DoctorScheduleVM { DoctorId = doctor.Id }
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _doctorService.DeleteDoctorAsync(id);
        if (result.Success)
        {
            TempData["Success"] = "Doctor deleted successfully.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSchedule(int doctorId, DoctorScheduleVM model)
    {
        var result = await _doctorService.AddScheduleAsync(new AddDoctorScheduleDto(
            doctorId,
            model.DayOfWeek,
            model.StartTime,
            model.EndTime
        ));

        if (result.Success)
        {
            TempData["Success"] = "Working shift added successfully.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Details), new { id = doctorId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSchedule(int doctorId, DoctorScheduleVM model)
    {
        var result = await _doctorService.UpdateScheduleAsync(new UpdateDoctorScheduleDto(
            model.Id,
            doctorId,
            model.DayOfWeek,
            model.StartTime,
            model.EndTime
        ));

        if (result.Success)
        {
            TempData["Success"] = "Working shift updated successfully.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Details), new { id = doctorId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSchedule(int doctorId, int scheduleId)
    {
        var result = await _doctorService.DeleteScheduleAsync(scheduleId);
        if (result.Success)
        {
            TempData["Success"] = "Working shift removed successfully.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Details), new { id = doctorId });
    }
}

