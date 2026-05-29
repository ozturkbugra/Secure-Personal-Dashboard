using System.ComponentModel.DataAnnotations;

namespace BugraLife.Models
{
    public class LoginUser
    {
        [Key]
        public int loginuser_id { get; set; }

        public string loginuser_username { get; set; }
        public string loginuser_namesurname { get; set; }
        public string login_password { get; set; }

        public bool IsTwoFactorEnabled { get; set; } = false; // 2FA Açık mı?
        public string? TwoFactorSecretKey { get; set; } // Google Auth için gizli anahtar
    }
}
