using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectDawnApi;

public class PlayerDM
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public bool IsBanned { get; set; } = false;

    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<RefreshTokenDM> RefreshTokens { get; set; } = new List<RefreshTokenDM>();

    public InventoryDM Inventory { get; set; } = null!;
}
