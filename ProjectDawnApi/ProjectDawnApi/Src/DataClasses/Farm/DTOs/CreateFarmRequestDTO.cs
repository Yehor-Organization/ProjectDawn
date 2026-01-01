using System.ComponentModel.DataAnnotations;

namespace ProjectDawnApi;

public class CreateFarmRequestDTO
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}