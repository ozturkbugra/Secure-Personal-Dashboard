using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class PersonController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public PersonController(BugraLifeDBContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            var persons = await _context.Persons.OrderBy(x => x.person_order).ToListAsync();

            // Sıralama mantığı (Otomatik artış için)
            int nextOrder = 1;
            if (persons.Any())
            {
                nextOrder = persons.Max(x => x.person_order) + 1;
            }
            ViewBag.NextOrder = nextOrder;

            return View(persons);
        }

        // 2. EKLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Person person)
        {
            if (ModelState.IsValid)
            {
                // İsim Kontrolü
                bool exists = await _context.Persons.AnyAsync(x => x.person_name == person.person_name);
                if (exists)
                {
                    return Json(new { success = false, message = "Bu isimde kayıt zaten var!" });
                }

                _context.Persons.Add(person);
                await _context.SaveChangesAsync();

                // Eklenen veriyi geri dönüyoruz (Tabloya basmak için)
                return Json(new { success = true, message = "Kişi başarıyla eklendi!", data = person });
            }
            return Json(new { success = false, message = "Veriler geçersiz." });
        }

        // 3. GÜNCELLEME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Person person)
        {
            // Checkbox işaretli değilse form veri göndermez, bu yüzden false gelir.
            // Bu normaldir, ek bir işleme gerek yok.

            if (ModelState.IsValid)
            {
                // İsim Çakışma Kontrolü
                bool exists = await _context.Persons.AnyAsync(x => x.person_name == person.person_name && x.person_id != person.person_id);
                if (exists)
                {
                    return Json(new { success = false, message = "Bu isimde başka bir kayıt var!" });
                }

                _context.Persons.Update(person);
                await _context.SaveChangesAsync();

                // Güncellenen veriyi geri dönüyoruz
                return Json(new { success = true, message = "Güncelleme başarılı!", data = person });
            }
            return Json(new { success = false, message = "Güncelleme başarısız." });
        }

        // 4. SİLME (Sayfa Yenilemeden)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var person = await _context.Persons.FindAsync(id);
            if (person != null)
            {
                _context.Persons.Remove(person);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Kayıt silindi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }
    }
}