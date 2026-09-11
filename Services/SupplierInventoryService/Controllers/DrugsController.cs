using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierInventoryService.DTOs;
using SupplierInventoryService.Services.Interfaces;

namespace SupplierInventoryService.Controllers;

[ApiController]
[Route("api/drugs")]
[Authorize] // any authenticated user by default (Doctor or Admin) — write actions override below
public class DrugsController : ControllerBase
{
    private readonly IDrugService _drugService;

    public DrugsController(IDrugService drugService)
    {
        _drugService = drugService;
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateDrugRequest request)
    {
        var result = await _drugService.CreateDrugAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var isAdmin = User.IsInRole("ADMIN");
        var result = await _drugService.GetAllDrugsAsync(includeInactive: isAdmin);
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var isAdmin = User.IsInRole("ADMIN");
        var result = await _drugService.GetDrugByIdAsync(id, includeInactive: isAdmin);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDrugRequest request)
    {
        var result = await _drugService.UpdateDrugAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _drugService.DeactivateDrugAsync(id);
        return NoContent();
    }
}