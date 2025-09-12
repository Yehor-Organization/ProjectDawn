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
        public int Id { get; set; }

        // Foreign key to the FarmDataModel this object belongs to
        public int FarmId { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // e.g., "Barn", "Fence", "Tree"

        // Storing 3D vector data as separate float properties
        public TransformationDataModel Transformation { get; set; } = new TransformationDataModel();
    }
}
