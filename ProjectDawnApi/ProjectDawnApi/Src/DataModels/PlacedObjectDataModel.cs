using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectDawnApi
{
    /// <summary>
    /// Represents a single object (like a barn, fence, tree) placed in a farm.
    /// </summary>
    public class PlacedObjectDataModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } = Guid.NewGuid(); 
        public int FarmId { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        // Unique per farm → safe to send over SignalR
        public Guid ObjectId { get; set; } = Guid.NewGuid();

        public TransformationDataModel Transformation { get; set; } = new TransformationDataModel();
    }

}
