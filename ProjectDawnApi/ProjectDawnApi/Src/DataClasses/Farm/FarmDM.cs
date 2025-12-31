using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectDawnApi;

public class FarmDM
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public int OwnerId { get; set; }
    public PlayerDM? Owner { get; set; }

    public ICollection<PlacedObjectDM> PlacedObjects { get; set; } = new List<PlacedObjectDM>();
    public ICollection<FarmVisitorDM> Visitors { get; set; } = new List<FarmVisitorDM>();
}
