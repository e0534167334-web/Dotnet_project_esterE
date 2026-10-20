using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface IChildRepository : IRepository<Child>
{
    Task<IReadOnlyList<Child>> GetByUserIdAsync(int userId, CancellationToken ct);
}
