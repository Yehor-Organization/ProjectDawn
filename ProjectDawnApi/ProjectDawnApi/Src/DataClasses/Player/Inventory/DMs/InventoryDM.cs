using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectDawnApi;

public class InventoryDM
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // One-to-one with Player
    public int PlayerId { get; set; }
    public PlayerDM Player { get; set; }

    public ICollection<InventoryItemDM> Items { get; set; }
        = new List<InventoryItemDM>();
}
