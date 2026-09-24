using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;

namespace BugraLife.Controllers
{
    [Authorize]
    public class LocationController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public LocationController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var list = await _context.Locations
                .OrderBy(x => x.location_name)
                .ToListAsync();

            return View(list);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Location location)
        {
            if (ModelState.IsValid)
            {
                // Kullanıcı tüm <iframe ...> etiketini yapıştırmış olabilir; sadece src'yi al.
                location.location_link = ExtractMapSrc(location.location_link);

                // Aynı isimde konum var mı?
                bool exists = await _context.Locations.AnyAsync(x => x.location_name == location.location_name);
                if (exists)
                {
                    return Json(new { success = false, message = "Bu konum adı zaten kayıtlı!" });
                }

                _context.Locations.Add(location);
                await _context.SaveChangesAsync();

                // Eklenen veriyi geri dönüyoruz (Tabloya basmak için)
                return Json(new { success = true, message = "Konum başarıyla eklendi.", data = location });
            }
            return Json(new { success = false, message = "Form verileri eksik." });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Location location)
        {
            if (ModelState.IsValid)
            {
                // Tüm <iframe ...> yapıştırıldıysa sadece src linkini sakla.
                location.location_link = ExtractMapSrc(location.location_link);

                // Kendisi hariç aynı isimde var mı?
                bool exists = await _context.Locations.AnyAsync(x => x.location_name == location.location_name && x.location_id != location.location_id);
                if (exists)
                {
                    return Json(new { success = false, message = "Bu isimde başka bir konum zaten var!" });
                }

                _context.Locations.Update(location);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi geri dönüyoruz
                return Json(new { success = true, message = "Konum güncellendi.", data = location });
            }
            return Json(new { success = false, message = "Güncelleme başarısız." });
        }

        // 4. SİLME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Locations.FindAsync(id);
            if (item != null)
            {
                _context.Locations.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Konum silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }

        // Yapıştırılan <iframe ...> etiketinden yalnızca src bağlantısını ayıklar.
        // Sadece düz link yapıştırıldıysa (iframe yoksa) olduğu gibi (kırpılmış) döner.
        private static string ExtractMapSrc(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var trimmed = input.Trim();

            // <iframe ... src="..."> içinden src'yi al (tek veya çift tırnak destekli)
            var match = Regex.Match(
                trimmed,
                @"<iframe[^>]*\ssrc\s*=\s*(?:""([^""]+)""|'([^']+)')",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var src = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                return src.Trim();
            }

            return trimmed;
        }

        // HARİTA GÖSTERİM SAYFASI
        public async Task<IActionResult> Maps(int? id)
        {
            ViewBag.LocationList = await _context.Locations
                .OrderBy(x => x.location_name)
                .ToListAsync();

            if (id.HasValue)
            {
                var selectedLocation = await _context.Locations.FindAsync(id);
                return View(selectedLocation);
            }

            return View(new Location());
        }
    }
}