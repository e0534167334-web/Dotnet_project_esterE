using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CreateChildRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly BirthDate { get; set; }
}

public class ChildResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
}
