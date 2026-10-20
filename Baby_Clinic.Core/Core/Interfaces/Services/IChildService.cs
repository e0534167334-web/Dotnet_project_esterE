using Core.Common;
using Core.DTOs;

namespace Core.Interfaces.Services;

public interface IChildService
{
    Task<Result<ChildResponse>> AddAsync(int parentId, CreateChildRequest request, CancellationToken ct);
    Task<Result<List<ChildResponse>>> GetByParentAsync(int parentId, CancellationToken ct);
}
