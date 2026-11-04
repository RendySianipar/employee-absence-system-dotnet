using EmployeeAbsence.Shared.Auth;
using EmployeeService.Api.Dtos;
using EmployeeService.Api.Services;
using EmployeeService.Api.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeService.Api.Controllers;

[ApiController]
[Route("employees")]
[Authorize]
public sealed class EmployeesController(IEmployeeService employeeService, IPhotoStorage photoStorage) : ControllerBase
{
    // ---- Self-service (any logged-in employee) ----

    [HttpGet("me")]
    public async Task<ActionResult<EmployeeResponse>> GetOwnProfile(CancellationToken cancellationToken) =>
        Ok(await employeeService.GetByIdAsync(User.GetEmployeeId(), cancellationToken));

    [HttpPut("me")]
    public async Task<ActionResult<EmployeeResponse>> UpdateOwnProfile(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await employeeService.UpdateOwnProfileAsync(User.GetEmployeeId(), request, cancellationToken));

    [HttpPost("me/photo")]
    [RequestSizeLimit(LocalPhotoStorage.MaxBytes + 64 * 1024)]
    public async Task<ActionResult<EmployeeResponse>> UploadPhoto(IFormFile file, CancellationToken cancellationToken)
    {
        var relativeUrl = await photoStorage.SaveAsync(file, cancellationToken);
        var photoUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";
        return Ok(await employeeService.UpdateOwnProfileAsync(
            User.GetEmployeeId(), new UpdateProfileRequest { PhotoUrl = photoUrl }, cancellationToken));
    }

    // ---- HR admin ----

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await employeeService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<EmployeeResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await employeeService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<EmployeeResponse>> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var created = await employeeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<EmployeeResponse>> Update(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken) =>
        Ok(await employeeService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await employeeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
