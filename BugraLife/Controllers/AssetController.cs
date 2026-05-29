using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class AssetController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public AssetController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var assets = await _context.Assets
                .Include(x => x.Ingredient)
                .Include(x => x.Person)
                .OrderByDescending(x => x.asset_date)
                .ToListAsync();

            ViewBag.Ingredients = await _context.Ingredients.OrderBy(x => x.ingredient_name).ToListAsync();
            ViewBag.Persons = await _context.Persons
                .Where(x => x.is_bank == false)
                .OrderBy(x => x.person_order)
                .ToListAsync();

            return View(assets);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Asset asset)
        {
            if (ModelState.IsValid)
            {
                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                // Eklenen veriyi detaylarıyla çek (Tabloya basmak için)
                var newAsset = await GetAssetDetails(asset.asset_id);

                return Json(new { success = true, message = "Varlık başarıyla eklendi!", data = newAsset });
            }
            return Json(new { success = false, message = "Form verileri eksik veya hatalı." });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Asset asset)
        {
            if (ModelState.IsValid)
            {
                _context.Assets.Update(asset);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi detaylarıyla çek
                var updatedAsset = await GetAssetDetails(asset.asset_id);

                return Json(new { success = true, message = "Varlık güncellendi!", data = updatedAsset });
            }
            return Json(new { success = false, message = "Güncelleme başarısız." });
        }

        // 4. SİLME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset != null)
            {
                _context.Assets.Remove(asset);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Varlık silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }

        // YARDIMCI METOD: ID'si verilen varlığın tüm detaylarını JSON formatına uygun hazırlar
        private async Task<object> GetAssetDetails(int id)
        {
            var item = await _context.Assets
                .Include(x => x.Ingredient)
                .Include(x => x.Person)
                .FirstOrDefaultAsync(x => x.asset_id == id);

            var trCulture = new CultureInfo("tr-TR");

            return new
            {
                id = item.asset_id,
                dateStr = item.asset_date.ToString("dd.MM.yyyy"),
                dateRaw = item.asset_date.ToString("yyyy-MM-dd"),
                person = item.Person != null ? item.Person.person_name : "-",
                personId = item.person_id,
                ingredient = item.Ingredient != null ? item.Ingredient.ingredient_name : "-",
                ingredientId = item.ingredient_id,
                desc = item.asset_description,
                amountRaw = item.asset_amount.ToString("N2", trCulture) // 1.500,00 formatı
            };
        }
    }
}