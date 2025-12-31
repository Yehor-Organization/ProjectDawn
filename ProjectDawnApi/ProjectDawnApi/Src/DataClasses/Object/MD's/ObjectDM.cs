using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectDawnApi;

public class ObjectDM
{
    [ForeignKey(nameof(FarmId))]
    public FarmDM Farm { get; set; } = null!;

    [Required]
    public int FarmId { get; set; }

    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public TransformationDM Transformation { get; set; } = new();

    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;
}