using ProjectDawnApi;
using ProjectDawnApi.Src.DataClasses.Visitor;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class FarmDM
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<ObjectDM> Objects { get; set; } = new List<ObjectDM>();
    public ICollection<PlayerDM> Owners { get; set; } = new List<PlayerDM>();
    public ICollection<VisitorDM> Visitors { get; set; } = new List<VisitorDM>();
}