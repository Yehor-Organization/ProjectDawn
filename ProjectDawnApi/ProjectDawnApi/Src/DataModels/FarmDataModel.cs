using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectDawnApi
{
    public class FarmDataModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int OwnerId { get; set; }
        public PlayerDataModel? Owner { get; set; }

        public ICollection<PlacedObjectDataModel> PlacedObjects { get; set; } = new List<PlacedObjectDataModel>();
        public ICollection<FarmVisitorDataModel> Visitors { get; set; } = new List<FarmVisitorDataModel>();
    }
}
