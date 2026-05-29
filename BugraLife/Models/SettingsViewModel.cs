namespace BugraLife.Models
{
    public class SettingsViewModel
    {
        // Profil Bilgileri
        public string Username { get; set; }
        public string NameSurname { get; set; }

        // Şifre Değiştirme
        public string? CurrentPassword { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }

        // 2FA Kısmı
        public bool IsTwoFactorEnabled { get; set; }
        public string? QrCodeImageUrl { get; set; } // QR Resmi
        public string? EntryKey { get; set; } // Manuel giriş kodu
        public string? VerificationCode { get; set; } // Kullanıcının girdiği kod
    }
}