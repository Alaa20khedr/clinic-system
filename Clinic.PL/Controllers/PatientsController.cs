using System.Linq;
using System.Threading.Tasks;
using Clinic.BLL.Interfaces;
using Clinic.BLL.Models;
using Clinic.PL.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.PL.Controllers;

public class PatientsController : Controller
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var items = await _patientService.GetPatientsAsync(search);
        var vm = new PatientListVM
        {
            Search = search,
            Patients = items.Select(p => new PatientListItemVM
            {
                Id = p.Id,
                FullName = p.FullName,
                DateOfBirth = p.DateOfBirth,
                PhoneNumber = p.PhoneNumber,
                AppointmentCount = p.AppointmentCount
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new PatientCreateEditVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatientCreateEditVM model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _patientService.CreatePatientAsync(new CreatePatientDto(
            model.FullName,
            model.DateOfBirth!.Value,
            model.PhoneNumber
        ));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not create patient.");
            return View(model);
        }

        TempData["Success"] = "Patient created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var patient = await _patientService.GetPatientByIdAsync(id);
        if (patient is null)
        {
            return NotFound();
        }

        var vm = new PatientCreateEditVM
        {
            Id = patient.Id,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            PhoneNumber = patient.PhoneNumber
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PatientCreateEditVM model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _patientService.UpdatePatientAsync(new UpdatePatientDto(
            model.Id,
            model.FullName,
            model.DateOfBirth!.Value,
            model.PhoneNumber
        ));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update patient.");
            return View(model);
        }

        TempData["Success"] = "Patient updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var patient = await _patientService.GetPatientByIdAsync(id);
        if (patient is null)
        {
            return NotFound();
        }

        var vm = new PatientDetailsVM
        {
            Id = patient.Id,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            PhoneNumber = patient.PhoneNumber,
            Appointments = patient.Appointments
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _patientService.DeletePatientAsync(id);
        if (result.Success)
        {
            TempData["Success"] = "Patient deleted successfully.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }
}

