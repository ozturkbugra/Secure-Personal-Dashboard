using System.Collections.Concurrent;

namespace BugraLife.Services
{
    /// <summary>
    /// Başarısız giriş denemelerini IP bazında takip eden ve kademeli (artan) kilit
    /// süresi uygulayan singleton servis. Bellekte tutulur (uygulama yeniden başlarsa sıfırlanır).
    ///
    /// Kural:
    ///  - İlk 4 hata: kilit yok.
    ///  - 5. hata: 2 dakika kilit.
    ///  - Sonrasında her 3 hatada bir kademe artar: 8. hata → 10 dk, 11. hata → 20 dk,
    ///    14. hata → 30 dk, 17. hata → 40 dk ... (kademe * 10 dk).
    ///  - Başarılı girişte o IP'nin kaydı temizlenir.
    /// </summary>
    public class LoginAttemptTracker
    {
        private class AttemptInfo
        {
            public int FailCount;
            public DateTime? LockoutEnd;
        }

        private readonly ConcurrentDictionary<string, AttemptInfo> _attempts = new();

        private const int InitialThreshold = 5;   // Kaçıncı hatada ilk kilit
        private const int InitialLockMinutes = 2; // İlk kilit süresi
        private const int TierStep = 3;           // Kaç hatada bir kademe artar
        private const int TierLockMinutes = 10;   // Kademe başına eklenen dakika (10, 20, 30...)

        /// <summary>Bu IP şu an kilitli mi? Kilitliyse kalan süreyi döner.</summary>
        public (bool locked, TimeSpan remaining) IsLocked(string key)
        {
            if (_attempts.TryGetValue(key, out var info) && info.LockoutEnd.HasValue)
            {
                var remaining = info.LockoutEnd.Value - DateTime.UtcNow;
                if (remaining > TimeSpan.Zero)
                    return (true, remaining);
            }
            return (false, TimeSpan.Zero);
        }

        /// <summary>Başarısız denemeyi kaydeder; yeni bir kilit tetiklendiyse süresini döner.</summary>
        public (bool nowLocked, TimeSpan lockDuration) RegisterFailure(string key)
        {
            var info = _attempts.GetOrAdd(key, _ => new AttemptInfo());

            lock (info)
            {
                info.FailCount++;

                int? lockMinutes = CalculateLockMinutes(info.FailCount);
                if (lockMinutes.HasValue)
                {
                    info.LockoutEnd = DateTime.UtcNow.AddMinutes(lockMinutes.Value);
                    return (true, TimeSpan.FromMinutes(lockMinutes.Value));
                }

                return (false, TimeSpan.Zero);
            }
        }

        /// <summary>Başarılı giriş: bu IP'nin tüm hata geçmişini temizler.</summary>
        public void Reset(string key)
        {
            _attempts.TryRemove(key, out _);
        }

        /// <summary>
        /// Verilen hata sayısı bir kilit tetikliyor mu, tetikliyorsa kaç dakika?
        /// Tetiklemiyorsa null döner.
        /// </summary>
        private static int? CalculateLockMinutes(int failCount)
        {
            if (failCount < InitialThreshold)
                return null;

            if (failCount == InitialThreshold)
                return InitialLockMinutes; // 5. hata → 2 dk

            // 5. hatadan sonrası: her TierStep hatada bir kademe.
            int extra = failCount - InitialThreshold; // 6,7,8... için 1,2,3...
            if (extra % TierStep == 0)
            {
                int tier = extra / TierStep; // 8.hata→1, 11.hata→2, 14.hata→3
                return tier * TierLockMinutes; // 10, 20, 30...
            }

            return null; // Ara hatalarda yeni kilit tetiklenmez (mevcut kilit sürebilir)
        }
    }
}
