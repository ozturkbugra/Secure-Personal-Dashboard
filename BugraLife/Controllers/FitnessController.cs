using BugraLife.DBContext;
using BugraLife.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BugraLife.Controllers
{
    [Authorize]
    public class FitnessController : Controller
    {
        private readonly BugraLifeDBContext _context;
        private readonly IWebHostEnvironment _env;

        public FitnessController(BugraLifeDBContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ============================================================
        // 1) HAREKETLER (CRUD + foto)
        // ============================================================
        public async Task<IActionResult> Exercises()
        {
            var list = await _context.FitnessExercises
                .AsNoTracking()
                .OrderBy(x => x.fitnessexercise_name)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> CreateExercise(string fitnessexercise_name, string? fitnessexercise_description, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(fitnessexercise_name))
                return Json(new { success = false, message = "Hareket adı zorunlu." });

            var ex = new FitnessExercise
            {
                fitnessexercise_name = fitnessexercise_name.Trim(),
                fitnessexercise_description = fitnessexercise_description,
                fitnessexercise_image = await SaveImageAsync(image)
            };

            _context.FitnessExercises.Add(ex);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hareket eklendi.", data = ToDto(ex) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> EditExercise(int id, string fitnessexercise_name, string? fitnessexercise_description, IFormFile? image, bool removeImage = false)
        {
            var ex = await _context.FitnessExercises.FindAsync(id);
            if (ex == null) return Json(new { success = false, message = "Hareket bulunamadı." });

            if (string.IsNullOrWhiteSpace(fitnessexercise_name))
                return Json(new { success = false, message = "Hareket adı zorunlu." });

            ex.fitnessexercise_name = fitnessexercise_name.Trim();
            ex.fitnessexercise_description = fitnessexercise_description;

            if (image != null && image.Length > 0)
            {
                DeleteImage(ex.fitnessexercise_image);
                ex.fitnessexercise_image = await SaveImageAsync(image);
            }
            else if (removeImage)
            {
                DeleteImage(ex.fitnessexercise_image);
                ex.fitnessexercise_image = null;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Hareket güncellendi.", data = ToDto(ex) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExercise(int id)
        {
            var ex = await _context.FitnessExercises.FindAsync(id);
            if (ex == null) return Json(new { success = false, message = "Hareket bulunamadı." });

            bool used = await _context.FitnessGroupExercises.AnyAsync(x => x.fitnessexercise_id == id);
            if (used)
                return Json(new { success = false, message = "Bu hareket bir veya daha fazla günde kullanılıyor. Önce gruplardan çıkarın." });

            DeleteImage(ex.fitnessexercise_image);
            _context.FitnessExercises.Remove(ex);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Hareket silindi." });
        }

        // ============================================================
        // 2) GÜNLER & GRUPLAMA
        // ============================================================
        public async Task<IActionResult> Days()
        {
            var days = await _context.FitnessDays
                .Include(d => d.Groups).ThenInclude(g => g.Exercises).ThenInclude(ge => ge.Exercise)
                .OrderBy(d => d.fitnessday_order)
                .ToListAsync();

            // Her grup/alternatif sıralı gelsin
            foreach (var d in days)
            {
                d.Groups = d.Groups.OrderBy(g => g.fitnessgroup_order).ToList();
                foreach (var g in d.Groups)
                    g.Exercises = g.Exercises.OrderByDescending(e => e.is_primary).ThenBy(e => e.fitnessgroupexercise_order).ToList();
            }

            ViewBag.AllExercises = await _context.FitnessExercises
                .AsNoTracking().OrderBy(x => x.fitnessexercise_name).ToListAsync();

            return View(days);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDay(string fitnessday_name)
        {
            if (string.IsNullOrWhiteSpace(fitnessday_name))
                return Json(new { success = false, message = "Gün adı zorunlu." });

            int nextOrder = (await _context.FitnessDays.MaxAsync(x => (int?)x.fitnessday_order) ?? 0) + 1;
            var day = new FitnessDay
            {
                fitnessday_name = fitnessday_name.Trim(),
                fitnessday_order = nextOrder,
                fitnessday_active = true
            };
            _context.FitnessDays.Add(day);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Gün eklendi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDay(int id, string fitnessday_name, bool fitnessday_active)
        {
            var day = await _context.FitnessDays.FindAsync(id);
            if (day == null) return Json(new { success = false, message = "Gün bulunamadı." });
            if (string.IsNullOrWhiteSpace(fitnessday_name))
                return Json(new { success = false, message = "Gün adı zorunlu." });

            day.fitnessday_name = fitnessday_name.Trim();
            day.fitnessday_active = fitnessday_active;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Gün güncellendi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDay(int id)
        {
            var day = await _context.FitnessDays
                .Include(d => d.Groups).ThenInclude(g => g.Exercises)
                .FirstOrDefaultAsync(d => d.fitnessday_id == id);
            if (day == null) return Json(new { success = false, message = "Gün bulunamadı." });

            foreach (var g in day.Groups)
                _context.FitnessGroupExercises.RemoveRange(g.Exercises);
            _context.FitnessGroups.RemoveRange(day.Groups);
            _context.FitnessDays.Remove(day);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Gün silindi." });
        }

        // Yeni grup (slot) ekle: ana hareket + opsiyonel alternatifler
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddGroup(int dayId, string? name, string? note, int primaryExerciseId, List<int>? altExerciseIds)
        {
            var day = await _context.FitnessDays.FindAsync(dayId);
            if (day == null) return Json(new { success = false, message = "Gün bulunamadı." });
            if (primaryExerciseId <= 0) return Json(new { success = false, message = "Ana hareket seçin." });

            int nextOrder = (await _context.FitnessGroups.Where(g => g.fitnessday_id == dayId)
                .MaxAsync(g => (int?)g.fitnessgroup_order) ?? 0) + 1;

            var group = new FitnessGroup
            {
                fitnessday_id = dayId,
                fitnessgroup_name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
                fitnessgroup_note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                fitnessgroup_order = nextOrder
            };
            group.Exercises.Add(new FitnessGroupExercise { fitnessexercise_id = primaryExerciseId, is_primary = true, fitnessgroupexercise_order = 0 });

            int o = 1;
            foreach (var altId in (altExerciseIds ?? new List<int>()).Distinct().Where(x => x != primaryExerciseId))
                group.Exercises.Add(new FitnessGroupExercise { fitnessexercise_id = altId, is_primary = false, fitnessgroupexercise_order = o++ });

            _context.FitnessGroups.Add(group);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Grup eklendi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroup(int groupId, string? name, string? note, int primaryExerciseId, List<int>? altExerciseIds)
        {
            var group = await _context.FitnessGroups
                .Include(g => g.Exercises)
                .FirstOrDefaultAsync(g => g.fitnessgroup_id == groupId);
            if (group == null) return Json(new { success = false, message = "Grup bulunamadı." });
            if (primaryExerciseId <= 0) return Json(new { success = false, message = "Ana hareket seçin." });

            group.fitnessgroup_name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
            group.fitnessgroup_note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

            _context.FitnessGroupExercises.RemoveRange(group.Exercises);
            group.Exercises.Clear();
            group.Exercises.Add(new FitnessGroupExercise { fitnessexercise_id = primaryExerciseId, is_primary = true, fitnessgroupexercise_order = 0 });
            int o = 1;
            foreach (var altId in (altExerciseIds ?? new List<int>()).Distinct().Where(x => x != primaryExerciseId))
                group.Exercises.Add(new FitnessGroupExercise { fitnessexercise_id = altId, is_primary = false, fitnessgroupexercise_order = o++ });

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Grup güncellendi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            var group = await _context.FitnessGroups
                .Include(g => g.Exercises)
                .FirstOrDefaultAsync(g => g.fitnessgroup_id == groupId);
            if (group == null) return Json(new { success = false, message = "Grup bulunamadı." });

            _context.FitnessGroupExercises.RemoveRange(group.Exercises);
            _context.FitnessGroups.Remove(group);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Grup silindi." });
        }

        // Sürükle-bırak sonrası grup sıralaması
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGroupOrder(List<int> groupIds)
        {
            if (groupIds == null || groupIds.Count == 0) return Json(new { success = false });
            var groups = await _context.FitnessGroups.Where(g => groupIds.Contains(g.fitnessgroup_id)).ToListAsync();
            for (int i = 0; i < groupIds.Count; i++)
            {
                var g = groups.FirstOrDefault(x => x.fitnessgroup_id == groupIds[i]);
                if (g != null) g.fitnessgroup_order = i + 1;
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ============================================================
        // 3) ANTRENMAN GÖRÜNÜMÜ (yapıldı takibi tarayıcıda)
        // ============================================================
        public async Task<IActionResult> Index(int? dayId)
        {
            var days = await _context.FitnessDays
                .Include(d => d.Groups).ThenInclude(g => g.Exercises).ThenInclude(ge => ge.Exercise)
                .Where(d => d.fitnessday_active)
                .OrderBy(d => d.fitnessday_order)
                .ToListAsync();

            foreach (var d in days)
            {
                d.Groups = d.Groups.OrderBy(g => g.fitnessgroup_order).ToList();
                foreach (var g in d.Groups)
                    g.Exercises = g.Exercises.OrderByDescending(e => e.is_primary).ThenBy(e => e.fitnessgroupexercise_order).ToList();
            }

            ViewBag.SelectedDayId = dayId ?? days.FirstOrDefault()?.fitnessday_id;
            return View(days);
        }

        // ============================================================
        // Yardımcılar
        // ============================================================
        private async Task<string?> SaveImageAsync(IFormFile? image)
        {
            if (image == null || image.Length == 0) return null;

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext)) return null;

            var dir = Path.Combine(_env.WebRootPath, "fitness");
            Directory.CreateDirectory(dir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, fileName);
            using (var fs = new FileStream(fullPath, FileMode.Create))
                await image.CopyToAsync(fs);

            return $"/fitness/{fileName}";
        }

        private void DeleteImage(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            try
            {
                var full = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            }
            catch { /* yut */ }
        }

        private static object ToDto(FitnessExercise ex) => new
        {
            ex.fitnessexercise_id,
            ex.fitnessexercise_name,
            ex.fitnessexercise_description,
            ex.fitnessexercise_image
        };
    }
}
