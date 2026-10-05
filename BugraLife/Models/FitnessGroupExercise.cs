using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BugraLife.Models
{
    // Grup (slot) ile hareket arasındaki bağ. is_primary = ana hareket, değilse alternatif.
    public class FitnessGroupExercise
    {
        [Key]
        public int fitnessgroupexercise_id { get; set; }

        [ForeignKey("Group")]
        public int fitnessgroup_id { get; set; }
        public virtual FitnessGroup? Group { get; set; }

        [ForeignKey("Exercise")]
        public int fitnessexercise_id { get; set; }
        public virtual FitnessExercise? Exercise { get; set; }

        public bool is_primary { get; set; }

        public int fitnessgroupexercise_order { get; set; }
    }
}
