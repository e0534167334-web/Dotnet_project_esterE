using Core.Common;
using Core.DTOs;

namespace Core.Interfaces.Services;

public interface ISlotService
{
    Task<Result<PagedResult<SlotResponse>>> SearchAsync(SlotSearchParams search, CancellationToken ct);
    Task<Result<SlotResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<SlotResponse>> CreateAsync(CreateSlotRequest request, CancellationToken ct);
    Task<Result> UpdateAsync(int id, CreateSlotRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result<List<VisitTypeResponse>>> GetVisitTypesAsync(CancellationToken ct);
}
