using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class MovementController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public MovementController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var list = await _context.Movements
                .Include(x => x.Debtor)
                .Include(x => x.Ingredient)
                .Include(x => x.Person)
                .OrderByDescending(x => x.movement_date)
                .ToListAsync();

            ViewBag.Debtors = await _context.Debtors.OrderBy(x => x.debtor_name).ToListAsync();
            ViewBag.Ingredients = await _context.Ingredients.OrderBy(x => x.ingredient_name).ToListAsync();
            ViewBag.Persons = await _context.Persons.Where(x => x.is_bank == false).OrderBy(x => x.person_order).ToListAsync();

            return View(list);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movement movement)
        {
            if (ModelState.IsValid)
            {
                _context.Movements.Add(movement);
                await _context.SaveChangesAsync();

                // Eklenen veriyi detaylarıyla çek (Tabloya basmak için)
                var newItem = await GetMovementDetails(movement.movement_id);

                return Json(new { success = true, message = "Hareket başarıyla kaydedildi.", data = newItem });
            }
            return Json(new { success = false, message = "Form verileri eksik." });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Movement movement)
        {
            if (ModelState.IsValid)
            {
                _context.Movements.Update(movement);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi detaylarıyla çek
                var updatedItem = await GetMovementDetails(movement.movement_id);

                return Json(new { success = true, message = "Hareket güncellendi.", data = updatedItem });
            }
            return Json(new { success = false, message = "Güncelleme başarısız." });
        }

        // 4. SİLME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Movements.FindAsync(id);
            if (item != null)
            {
                _context.Movements.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Kayıt silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }

        // YARDIMCI METOD: Detaylı veriyi JSON için hazırlar
        private async Task<object> GetMovementDetails(int id)
        {
            var item = await _context.Movements
                .Include(x => x.Debtor)
                .Include(x => x.Ingredient)
                .Include(x => x.Person)
                .FirstOrDefaultAsync(x => x.movement_id == id);

            var trCulture = new CultureInfo("tr-TR");

            return new
            {
                id = item.movement_id,
                dateStr = item.movement_date.ToString("dd.MM.yyyy"),
                dateRaw = item.movement_date.ToString("yyyy-MM-dd"),
                debtor = item.Debtor != null ? item.Debtor.debtor_name : "-",
                debtorId = item.debtor_id,
                ingredient = item.Ingredient != null ? item.Ingredient.ingredient_name : "-",
                ingredientId = item.ingredient_id,
                person = item.Person != null ? item.Person.person_name : "-",
                personId = item.person_id,
                desc = item.movement_description,
                amountRaw = item.movement_amount.ToString("N2", trCulture)
            };
        }
    }
}