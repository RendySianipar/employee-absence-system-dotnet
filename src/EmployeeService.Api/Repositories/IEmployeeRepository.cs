using EmployeeService.Api.Domain;

namespace EmployeeService.Api.Repositories;

public interface IEmployeeRepository
{
    Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByEmailAsync(string companyEmail, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string companyEmail, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    void Remove(Employee employee);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
