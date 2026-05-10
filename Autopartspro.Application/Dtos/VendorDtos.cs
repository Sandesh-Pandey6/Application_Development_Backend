using System.ComponentModel.DataAnnotations;

namespace Autopartspro.Application.Dtos;

public record VendorDto(
    int Id,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public class VendorUpsertDto
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ContactPerson { get; set; }

    [MaxLength(150), EmailAddress]
    public string? Email { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }
}
