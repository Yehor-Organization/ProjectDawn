using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi;

[Index(nameof(InventoryId), nameof(ItemType), IsUnique = true)]
public class InventoryItemDM
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(InventoryId))]
    public InventoryDM Inventory { get; set; }

    [Required]
    public int InventoryId { get; set; }

    [Required]
    [StringLength(50)]
    public string ItemType { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }
}