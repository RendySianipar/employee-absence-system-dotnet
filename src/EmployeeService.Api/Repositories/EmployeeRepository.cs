using EmployeeService.Api.Data;
using EmployeeService.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Api.Repositories;

public class EmployeeRepository(EmployeeDbContext db) : IEmployeeRepository
{
    public async Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Employees.AsNoTracking().OrderBy(e => e.Name).ToListAsync(cancellationToken);

    public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Employee?> GetByEmailAsync(string companyEmail, CancellationToken cancellationToken = default) =>
        db.Employees.FirstOrDefaultAsync(e => e.CompanyEmail == companyEmail, cancellationToken);

    public Task<bool> EmailExistsAsync(string companyEmail, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        db.Employees.AnyAsync(e => e.CompanyEmail == companyEmail && (excludeId == null || e.Id != excludeId), cancellationToken);

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default) =>
        await db.Employees.AddAsync(employee, cancellationToken);

    public void Remove(Employee employee) => db.Employees.Remove(employee);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
