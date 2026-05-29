using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.DBContext;
using BugraLife.Models;

namespace BugraLife.Controllers
{
    public class PracticalNoteController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public PracticalNoteController(BugraLifeDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // En son eklenen en üstte olsun
            var notes = await _context.PracticalNotes
                                      .OrderByDescending(x => x.created_at)
                                      .ToListAsync();
            return View(notes);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PracticalNote model)
        {
            if (ModelState.IsValid)
            {
                model.created_at = DateTime.Now; // Tarihi ata
                _context.Add(model);
                await _context.SaveChangesAsync();

                // Eklenen veriyi ve formatlı tarihi geri dön
                return Json(new
                {
                    success = true,
                    message = "Not başarıyla eklendi.",
                    data = new
                    {
                        id = model.practicalnote_id,
                        title = model.practicalnote_title,
                        desc = model.practicalnote_description,
                        dateStr = model.created_at.ToString("dd.MM.yyyy HH:mm")
                    }
                });
            }
            return Json(new { success = false, message = "Eksik alanlar var." });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PracticalNote model)
        {
            var note = await _context.PracticalNotes.FindAsync(model.practicalnote_id);
            if (note != null)
            {
                note.practicalnote_title = model.practicalnote_title;
                note.practicalnote_description = model.practicalnote_description;
                // Tarihi güncellemiyoruz, oluşturulma tarihi kalsın.

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Not güncellendi.",
                    data = new
                    {
                        id = note.practicalnote_id,
                        title = note.practicalnote_title,
                        desc = note.practicalnote_description,
                        dateStr = note.created_at.ToString("dd.MM.yyyy HH:mm")
                    }
                });
            }
            return Json(new { success = false, message = "Not bulunamadı." });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var note = await _context.PracticalNotes.FindAsync(id);
            if (note != null)
            {
                _context.Remove(note);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Not silindi." });
            }
            return Json(new { success = false, message = "Hata oluştu." });
        }
    }
}