using ProjectDawnApi;
using System.ComponentModel.DataAnnotations;

public class ObjectDM
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public int FarmId { get; set; }

    public FarmDM Farm { get; set; } = null!;

    public TransformationDM Transformation { get; set; } = new();

    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
}