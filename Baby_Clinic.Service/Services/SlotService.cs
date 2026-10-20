using AutoMapper;
using Core.Common;
using Core.DTOs;
using Core.Entities;
using Core.Exceptions;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Baby_Clinic.Service.Services;

public class SlotService : ISlotService
{
    private readonly ISlotRepository _slots;
    private readonly IVisitTypeRepository _visitTypes;
    private readonly IMapper _mapper;

    public SlotService(ISlotRepository slots, IVisitTypeRepository visitTypes, IMapper mapper)
    {
        _slots = slots;
        _visitTypes = visitTypes;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<SlotResponse>>> SearchAsync(SlotSearchParams search, CancellationToken ct)
    {
        if (search.From.HasValue && search.To.HasValue && search.From > search.To)
            return Result<PagedResult<SlotResponse>>.Fail(ResultStatus.Validation, "'From' must be earlier than 'To'.");

        var (items, totalCount) = await _slots.SearchAsync(search, ct);

        return Result<PagedResult<SlotResponse>>.Success(new PagedResult<SlotResponse>
        {
            Items = _mapper.Map<List<SlotResponse>>(items),
            TotalCount = totalCount,
            Page = search.Page,
            PageSize = search.PageSize
        });
    }

    public async Task<Result<SlotResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var slot = await _slots.GetWithVisitTypesAsync(id, ct);
        return slot is null
            ? Result<SlotResponse>.Fail(ResultStatus.NotFound, $"Slot {id} was not found.")
            : Result<SlotResponse>.Success(_mapper.Map<SlotResponse>(slot));
    }

    public async Task<Result<SlotResponse>> CreateAsync(CreateSlotRequest request, CancellationToken ct)
    {
        var startTimeUtc = ToUtc(request.StartTimeUtc);
        if (startTimeUtc <= DateTime.UtcNow)
            return Result<SlotResponse>.Fail(ResultStatus.Validation, "Slot start time must be in the future.");

        var visitTypes = await LoadVisitTypesAsync(request.VisitTypeIds, ct);
        if (visitTypes is null)
            return Result<SlotResponse>.Fail(ResultStatus.Validation, "One or more visit types do not exist.");

        var slot = _mapper.Map<Slot>(request);
        slot.StartTimeUtc = startTimeUtc;
        slot.VisitTypes = visitTypes;

        await _slots.AddAsync(slot, ct);
        await _slots.SaveChangesAsync(ct);

        return Result<SlotResponse>.Success(_mapper.Map<SlotResponse>(slot));
    }

    public async Task<Result> UpdateAsync(int id, CreateSlotRequest request, CancellationToken ct)
    {
        var slot = await _slots.GetWithVisitTypesForUpdateAsync(id, ct);
        if (slot is null)
            return Result.Fail(ResultStatus.NotFound, $"Slot {id} was not found.");

        var startTimeUtc = ToUtc(request.StartTimeUtc);
        if (startTimeUtc <= DateTime.UtcNow)
            return Result.Fail(ResultStatus.Validation, "Slot start time must be in the future.");

        var visitTypes = await LoadVisitTypesAsync(request.VisitTypeIds, ct);
        if (visitTypes is null)
            return Result.Fail(ResultStatus.Validation, "One or more visit types do not exist.");

        _mapper.Map(request, slot);
        slot.StartTimeUtc = startTimeUtc;

        // ניהול סוגי הביקור של המשבצת: מחליפים את הרשימה בקשר ה-many-to-many
        slot.VisitTypes.Clear();
        slot.VisitTypes.AddRange(visitTypes);

        try
        {
            await _slots.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Fail(ResultStatus.Conflict, "The slot was modified by another request. Please reload and try again.");
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var slot = await _slots.GetByIdAsync(id, ct);
        if (slot is null)
            return Result.Fail(ResultStatus.NotFound, $"Slot {id} was not found.");

        // גם תור שבוטל חוסם מחיקה: ה-FK מוגדר Restrict וההיסטוריה נשמרת
        if (await _slots.HasAppointmentsAsync(id, ct))
            return Result.Fail(ResultStatus.Conflict, "Cannot delete a slot that has appointments.");

        _slots.Remove(slot);

        try
        {
            await _slots.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Fail(ResultStatus.Conflict, "The slot was modified by another request. Please reload and try again.");
        }

        return Result.Success();
    }

    public async Task<Result<List<VisitTypeResponse>>> GetVisitTypesAsync(CancellationToken ct)
    {
        var visitTypes = await _visitTypes.GetAllAsync(ct);
        return Result<List<VisitTypeResponse>>.Success(_mapper.Map<List<VisitTypeResponse>>(visitTypes));
    }

    // null אם הרשימה ריקה או שאחד המזהים לא קיים
    private async Task<List<VisitType>?> LoadVisitTypesAsync(List<int> ids, CancellationToken ct)
    {
        var distinctIds = ids.Distinct().ToList();
        if (distinctIds.Count == 0)
            return null;

        var visitTypes = await _visitTypes.GetByIdsAsync(distinctIds, ct);
        return visitTypes.Count == distinctIds.Count ? visitTypes.ToList() : null;
    }

    // עמודת timestamptz דורשת DateTime מסוג Utc; ערך בלי אזור זמן מתפרש כ-UTC
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
