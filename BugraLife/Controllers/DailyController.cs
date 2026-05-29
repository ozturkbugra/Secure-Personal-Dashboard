using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class DailyController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public DailyController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // --- TAKVİM MODU (Günlük 1) ---
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Dailies.ToListAsync();
            var formattedEvents = events.Select(x => FormatDailyToEvent(x)).ToList();
            return Json(formattedEvents);
        }

        // --- OKUMA MODU (Kitap Sayfası / Günlük 2) ---
        public IActionResult Read()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetByDate(DateTime date)
        {
            var item = await _context.Dailies.FirstOrDefaultAsync(x => x.daily_date.Date == date.Date);
            if (item != null)
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = item.daily_id,
                        date = item.daily_date.ToString("yyyy-MM-dd"),
                        description = item.daily_description,
                        status = (int)item.daily_status
                    }
                });
            }
            return Json(new { success = false });
        }

        // --- ORTAK CRUD İŞLEMLERİ (Hem Takvim Hem Okuma Modu İçin) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Daily daily)
        {
            if (!string.IsNullOrEmpty(daily.daily_description))
            {
                bool exists = await _context.Dailies.AnyAsync(x => x.daily_date.Date == daily.daily_date.Date);
                if (exists) return Json(new { success = false, message = "Bugün için zaten kayıt var. Var olanı düzenleyin." });

                _context.Dailies.Add(daily);
                await _context.SaveChangesAsync();

                var eventData = FormatDailyToEvent(daily);
                return Json(new { success = true, message = "Günlük kaydedildi!", data = eventData });
            }
            return Json(new { success = false, message = "Bir şeyler yazmalısın." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Daily daily)
        {
            var item = await _context.Dailies.FindAsync(daily.daily_id);
            if (item != null)
            {
                item.daily_description = daily.daily_description;
                item.daily_status = daily.daily_status;
                await _context.SaveChangesAsync();

                var eventData = FormatDailyToEvent(item);
                return Json(new { success = true, message = "Güncellendi.", data = eventData });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Dailies.FindAsync(id);
            if (item != null)
            {
                _context.Dailies.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Silindi." });
            }
            return Json(new { success = false, message = "Hata." });
        }

        // --- YARDIMCI METOD ---
        private object FormatDailyToEvent(Daily x)
        {
            string title = "";
            string color = "";

            switch (x.daily_status)
            {
                case DailyStatus.Kotu: title = "😡 Kötü"; color = "#dc3545"; break;
                case DailyStatus.Orta: title = "😐 Orta"; color = "#fd7e14"; break;
                case DailyStatus.Iyi: title = "🙂 İyi"; color = "#0d6efd"; break;
                case DailyStatus.Super: title = "🤩 Süper"; color = "#198754"; break;
                default: title = "-"; color = "#6c757d"; break;
            }

            return new
            {
                id = x.daily_id,
                title = title,
                start = x.daily_date.ToString("yyyy-MM-dd"),
                backgroundColor = color,
                borderColor = "transparent",
                extendedProps = new
                {
                    description = x.daily_description,
                    statusId = (int)x.daily_status
                }
            };
        }
    }
}