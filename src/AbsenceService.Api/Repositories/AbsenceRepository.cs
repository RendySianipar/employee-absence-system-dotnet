using AbsenceService.Api.Data;
using AbsenceService.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AbsenceService.Api.Repositories;

public class AbsenceRepository(AbsenceDbContext db) : IAbsenceRepository
{
    public Task<Absence?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Absences.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Absence>> GetForEmployeeOnDateAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default) =>
        await db.Absences.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Date == date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Absence>> GetForEmployeeAsync(Guid employeeId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) =>
        await db.Absences.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Date >= start && a.Date <= end)
            .OrderBy(a => a.Date).ThenBy(a => a.Time)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Absence>> GetAllAsync(DateOnly? start, DateOnly? end, CancellationToken cancellationToken = default)
    {
        var query = db.Absences.AsNoTracking();
        if (start is not null) query = query.Where(a => a.Date >= start);
        if (end is not null) query = query.Where(a => a.Date <= end);

        return await query.OrderByDescending(a => a.Date).ThenByDescending(a => a.Time).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Absence absence, CancellationToken cancellationToken = default) =>
        await db.Absences.AddAsync(absence, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
