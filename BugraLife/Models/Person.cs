using System.ComponentModel.DataAnnotations;

namespace BugraLife.Models
{
    public class Person
    {
        [Key]
        public int person_id { get; set; }
        public string person_name { get; set; }
        public int person_order { get; set; }
        public bool is_bank { get; set; }
    }
}
