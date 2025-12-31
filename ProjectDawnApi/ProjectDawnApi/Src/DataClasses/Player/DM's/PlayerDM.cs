using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectDawnApi;

public class PlayerDM
{
    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<FarmDM> Farms { get; set; } = new List<FarmDM>();

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public InventoryDM Inventory { get; set; } = null!;

    public bool IsBanned { get; set; } = false;

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<RefreshTokenDM> RefreshTokens { get; set; } = new List<RefreshTokenDM>();
}