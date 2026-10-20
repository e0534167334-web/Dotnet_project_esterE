using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs;

// משמש גם ליצירה (POST) וגם לעדכון (PUT)
public class CreateSlotRequest
{
    [Required]
    public DateTime StartTimeUtc { get; set; }

    [Required, StringLength(100)]
    public string NurseName { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public List<int> VisitTypeIds { get; set; } = new();
}

public class SlotResponse
{
    public int Id { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public string NurseName { get; set; } = string.Empty;
    public SlotStatus Status { get; set; }
    public List<VisitTypeResponse> VisitTypes { get; set; } = new();
}

public class SlotSearchParams
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? VisitTypeId { get; set; }
    public bool AvailableOnly { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}

public class VisitTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
