using AutoMapper;
using Core.Common;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Baby_Clinic.Service.Services;

public class ChildService : IChildService
{
    private readonly IChildRepository _children;
    private readonly IMapper _mapper;

    public ChildService(IChildRepository children, IMapper mapper)
    {
        _children = children;
        _mapper = mapper;
    }

    public async Task<Result<ChildResponse>> AddAsync(int parentId, CreateChildRequest request, CancellationToken ct)
    {
        if (request.BirthDate > DateOnly.FromDateTime(DateTime.UtcNow))
            return Result<ChildResponse>.Fail(ResultStatus.Validation, "Birth date cannot be in the future.");

        var child = _mapper.Map<Child>(request);
        child.UserId = parentId; // ההורה נקבע לפי המשתמש המחובר, לא לפי גוף הבקשה

        await _children.AddAsync(child, ct);
        await _children.SaveChangesAsync(ct);

        return Result<ChildResponse>.Success(_mapper.Map<ChildResponse>(child));
    }

    // כל הורה רואה רק את הילדים שלו
    public async Task<Result<List<ChildResponse>>> GetByParentAsync(int parentId, CancellationToken ct)
    {
        var children = await _children.GetByUserIdAsync(parentId, ct);
        return Result<List<ChildResponse>>.Success(_mapper.Map<List<ChildResponse>>(children));
    }
}
