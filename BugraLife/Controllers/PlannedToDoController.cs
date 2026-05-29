using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BugraLife.Models;
using BugraLife.DBContext;
using Microsoft.AspNetCore.Authorization;

namespace BugraLife.Controllers
{
    [Authorize]
    public class PlannedToDoController : Controller
    {
        private readonly BugraLifeDBContext _context;

        public PlannedToDoController(BugraLifeDBContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.PlannedToDos.Select(x => new
            {
                id = x.plannedtodo_id,
                title = x.plannedtodo_description,
                start = x.plannedtodo_date.ToString("yyyy-MM-dd"), // FullCalendar formatı
                // Eğer yapıldıysa yeşil, yapılmadıysa mavi/tema rengi
                className = x.plannedtodo_done ? "fc-event-done" : "fc-event-todo",
                extendedProps = new
                {
                    description = x.plannedtodo_description,
                    isDone = x.plannedtodo_done
                }
            }).ToListAsync();

            return Json(events);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDate(int id, DateTime newDate)
        {
            var item = await _context.PlannedToDos.FindAsync(id);
            if (item != null)
            {
                item.plannedtodo_date = newDate;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Tarih güncellendi." });
            }
            return Json(new { success = false, message = "Kayıt bulunamadı." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlannedToDo todo)
        {
            if (!string.IsNullOrEmpty(todo.plannedtodo_description))
            {
                todo.plannedtodo_done = false; 
                _context.PlannedToDos.Add(todo);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Planlandı!" });
            }
            return Json(new { success = false, message = "Açıklama giriniz." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PlannedToDo todo)
        {
            var item = await _context.PlannedToDos.FindAsync(todo.plannedtodo_id);
            if (item != null)
            {
                item.plannedtodo_description = todo.plannedtodo_description;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Güncellendi." });
            }
            return Json(new { success = false, message = "Hata." });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var item = await _context.PlannedToDos.FindAsync(id);
            if (item != null)
            {
                item.plannedtodo_done = !item.plannedtodo_done;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Durum değişti." });
            }
            return Json(new { success = false, message = "Hata." });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.PlannedToDos.FindAsync(id);
            if (item != null)
            {
                _context.PlannedToDos.Remove(item);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Silindi." });
            }
            return Json(new { success = false, message = "Hata." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int plannedtodo_id)
        {
            var todo = await _context.PlannedToDos.FindAsync(plannedtodo_id);

            if (todo != null)
            {
                todo.plannedtodo_done = true;
                _context.PlannedToDos.Update(todo);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Home");
        }

    }
}