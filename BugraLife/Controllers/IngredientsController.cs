using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class IngredientsController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public IngredientsController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var ingredients = await _context.Ingredients.ToListAsync();
            return View(ingredients);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ingredient ingredient)
        {
            if (ModelState.IsValid)
            {
                // Aynı isimde kayıt kontrolü
                bool exists = await _context.Ingredients.AnyAsync(x => x.ingredient_name == ingredient.ingredient_name);

                if (exists)
                {
                    return Json(new { success = false, message = "Bu malzeme zaten kayıtlı! Lütfen farklı bir isim giriniz." });
                }

                _context.Ingredients.Add(ingredient);
                await _context.SaveChangesAsync();

                // Eklenen veriyi geri dönüyoruz (Tabloya eklemek için)
                return Json(new { success = true, message = "Malzeme başarıyla eklendi!", data = ingredient });
            }
            return Json(new { success = false, message = "Form verileri geçersiz." });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Ingredient ingredient)
        {
            if (ModelState.IsValid)
            {
                // Kendisi hariç aynı isim kontrolü
                bool exists = await _context.Ingredients.AnyAsync(x => x.ingredient_name == ingredient.ingredient_name && x.ingredient_id != ingredient.ingredient_id);

                if (exists)
                {
                    return Json(new { success = false, message = "Bu isimde başka bir malzeme zaten var!" });
                }

                _context.Ingredients.Update(ingredient);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi geri dönüyoruz
                return Json(new { success = true, message = "Malzeme güncellendi!", data = ingredient });
            }
            return Json(new { success = false, message = "Güncelleme başarısız!" });
        }

        // 4. SİLME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ingredient = await _context.Ingredients.FindAsync(id);
            if (ingredient != null)
            {
                _context.Ingredients.Remove(ingredient);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Malzeme silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }
    }
}