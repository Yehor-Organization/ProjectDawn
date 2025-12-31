using System.ComponentModel.DataAnnotations;

namespace ProjectDawnApi;

public class PlaceObjectDTO
{
    [Required]
    public TransformationDM Transformation { get; set; } = new();

    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
}