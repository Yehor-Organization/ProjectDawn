using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectDawnApi
{
    public class InventoryItemDataModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public PlayerDataModel Player { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string ItemName { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }
}
