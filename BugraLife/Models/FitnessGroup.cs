using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BugraLife.Models
{
    // Bir gün içindeki tek bir egzersiz slotu. Slotta bir ana hareket (is_primary) ve
    // alet doluysa/bozuksa kullanılacak opsiyonel alternatif hareketler bulunur.
    public class FitnessGroup
    {
        [Key]
        public int fitnessgroup_id { get; set; }

        [ForeignKey("Day")]
        public int fitnessday_id { get; set; }
        public virtual FitnessDay? Day { get; set; }

        // Opsiyonel grup etiketi (ör. "Göğüs Press")
        public string? fitnessgroup_name { get; set; }

        // Opsiyonel: o güne özel set/ağırlık notu
        public string? fitnessgroup_note { get; set; }

        public int fitnessgroup_order { get; set; }

        public virtual ICollection<FitnessGroupExercise> Exercises { get; set; } = new List<FitnessGroupExercise>();
    }
}
