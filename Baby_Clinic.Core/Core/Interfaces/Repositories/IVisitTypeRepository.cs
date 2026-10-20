using Core.Entities;

namespace Core.Interfaces.Repositories;

public interface IVisitTypeRepository : IRepository<VisitType>
{
    Task<IReadOnlyList<VisitType>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<VisitType>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct);
}
