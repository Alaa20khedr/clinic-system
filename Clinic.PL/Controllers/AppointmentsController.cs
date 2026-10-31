using System;
using System.Linq;
using System.Threading.Tasks;
using Clinic.BLL.Interfaces;
using Clinic.BLL.Models;
using Clinic.PL.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.PL.Controllers;

public class AppointmentsController : Controller
{
    private readonly IAppointmentService _service;
    private readonly IPatientService _patientService;

    public AppointmentsController(IAppointmentService service, IPatientService patientService)
    {
        _service = service;
        _patientService = patientService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? filterDate, int? filterDoctorId)
    {
        var vm = new AppointmentsPageVM
        {
            Doctors = await _service.GetDoctorsAsync(),
            Appointments = await _service.GetAppointmentsAsync(filterDate, filterDoctorId),
            FilterDate = filterDate,
            FilterDoctorId = filterDoctorId
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new CreateAppointmentVM
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Doctors = await _service.GetDoctorsAsync()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> SearchPatients(string? term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            return Json(Array.Empty<object>());

        var patients = await _patientService.GetPatientsAsync(term);
        return Json(patients.Take(10).Select(p => new
        {
            id = p.Id,
            name = p.FullName,
            phone = p.PhoneNumber,
            dob = p.DateOfBirth.ToString("yyyy-MM-dd")
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAppointmentVM form)
    {
        if (!ModelState.IsValid)
        {
            form.Doctors = await _service.GetDoctorsAsync();
            return View(form);
        }

        var result = await _service.BookAsync(new BookAppointmentRequest
        {
            PatientId = form.PatientId,
            DoctorId = form.DoctorId,
            Date = form.Date,
            StartTime = form.StartTime!.Value,
            PatientFullName = form.PatientFullName,
            DateOfBirth = form.DateOfBirth!.Value,
            PhoneNumber = form.PhoneNumber
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not book the appointment.");
            form.Doctors = await _service.GetDoctorsAsync();
            return View(form);
        }

        TempData["Success"] = "Appointment booked successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetFreeSlots(int doctorId, DateOnly date)
    {
        var slots = await _service.GetFreeSlotsAsync(doctorId, date);
        return Json(slots.Select(s => new { value = s.Value, label = s.Label }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, DateOnly? filterDate, int? filterDoctorId)
    {
        var result = await _service.CancelAsync(id);
        if (result.Success)
        {
            TempData["Success"] = "Appointment cancelled successfully.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index), new
        {
            filterDate = filterDate?.ToString("yyyy-MM-dd"),
            filterDoctorId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, DateOnly? filterDate, int? filterDoctorId)
    {
        var result = await _service.CompleteAsync(id);
        if (result.Success)
        {
            TempData["Success"] = "Appointment marked as completed.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index), new
        {
            filterDate = filterDate?.ToString("yyyy-MM-dd"),
            filterDoctorId
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _service.GetAppointmentDetailsAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        var vm = new AppointmentDetailsVM
        {
            Id = item.Id,
            PatientId = item.PatientId,
            PatientFullName = item.PatientFullName,
            PatientPhoneNumber = item.PatientPhoneNumber,
            PatientDateOfBirth = item.PatientDateOfBirth,
            DoctorId = item.DoctorId,
            DoctorName = item.DoctorName,
            DoctorSpecialty = item.DoctorSpecialty,
            Date = item.Date,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            Status = item.Status
        };

        return View(vm);
    }
}
