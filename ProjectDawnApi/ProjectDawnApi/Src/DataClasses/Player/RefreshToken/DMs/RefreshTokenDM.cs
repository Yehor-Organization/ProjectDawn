using System.ComponentModel.DataAnnotations;

namespace ProjectDawnApi;

public class RefreshTokenDM
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public int PlayerId { get; set; }
    public PlayerDM Player { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public bool IsRevoked { get; set; }
}
