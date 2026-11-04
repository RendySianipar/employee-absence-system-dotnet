using AbsenceService.Api.Domain;

namespace AbsenceService.Api.Repositories;

public interface IAbsenceRepository
{
    Task<Absence?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Absence>> GetForEmployeeOnDateAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Absence>> GetForEmployeeAsync(Guid employeeId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Absence>> GetAllAsync(DateOnly? start, DateOnly? end, CancellationToken cancellationToken = default);
    Task AddAsync(Absence absence, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
