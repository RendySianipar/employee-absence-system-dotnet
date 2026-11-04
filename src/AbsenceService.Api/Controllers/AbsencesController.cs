using AbsenceService.Api.Dtos;
using AbsenceService.Api.Services;
using EmployeeAbsence.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbsenceService.Api.Controllers;

[ApiController]
[Route("absences")]
[Authorize]
public sealed class AbsencesController(IAbsenceService absenceService) : ControllerBase
{
    /// <summary>Check in ("masuk") or check out ("pulang") for the logged-in employee.</summary>
    [HttpPost]
    [ProducesResponseType<AbsenceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AbsenceResponse>> Create(CreateAbsenceRequest request, CancellationToken cancellationToken)
    {
        var created = await absenceService.RecordAsync(User.GetEmployeeId(), request.Status, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Own attendance per day. Defaults to the current month.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<IReadOnlyList<DailyAttendanceSummary>>> GetSummary(
        [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, CancellationToken cancellationToken) =>
        Ok(await absenceService.GetSummaryAsync(User.GetEmployeeId(), startDate, endDate, cancellationToken));

    /// <summary>HR admin: read-only list of all employees' attendance.</summary>
    [HttpGet("admin")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IReadOnlyList<AbsenceResponse>>> GetAllForAdmin(
        [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, CancellationToken cancellationToken) =>
        Ok(await absenceService.GetAllForAdminAsync(startDate, endDate, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AbsenceResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await absenceService.GetByIdAsync(id, User.GetEmployeeId(), User.IsAdmin(), cancellationToken));
}
