using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.StaticFiles;

namespace BugraLife.Controllers
{
    // Varsayılan olarak her yer kilitli (Sadece sen girebilirsin)
    [Authorize]
    public class TransferController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public TransferController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // ==========================================
        // 1. HALKA AÇIK KISIM (MİSAFİR)
        // ==========================================
        [AllowAnonymous] // <--- Şifresiz giriş
        public IActionResult Index()
        {
            var folderPath = Path.Combine(_env.WebRootPath, "transfer");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            // GetFileList'ten gelen listeyi CreatedDate'e göre tersten (descending) sıralıyoruz
            var fileList = GetFileList(folderPath)
                            .OrderByDescending(x => x.CreatedDate) // <--- İŞTE BU SATIR
                            .ToList();

            return View(fileList);
        }

        [AllowAnonymous] // <--- Şifresiz indirme
        public IActionResult Download(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return Content("Dosya adı hatalı.");

            var folderPath = Path.Combine(_env.WebRootPath, "transfer");
            var filePath = Path.Combine(folderPath, fileName);

            if (!System.IO.File.Exists(filePath) || !filePath.StartsWith(folderPath))
                return Content("Dosya bulunamadı.");

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out string contentType)) contentType = "application/octet-stream";

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, contentType, fileName);
        }

        // ==========================================
        // 2. SENİN YÖNETİM PANELİN (ADMİN)
        // ==========================================

        // Bu sayfaya sadece giriş yapmış (Authorize) kullanıcılar girebilir
        public IActionResult Manage()
        {
            var folderPath = Path.Combine(_env.WebRootPath, "transfer");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var fileList = GetFileList(folderPath);
            return View(fileList);
        }

        [HttpPost]
        [DisableRequestSizeLimit] // 1. Dosya boyutu limitini (30MB) kaldırır
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)] // 2. Form veri limitini sonsuz yapar
        public async Task<IActionResult> Upload()
        {
            // Parametre (List<IFormFile> files) kullanmıyoruz, direkt gelen ham dosyaları alıyoruz.
            // Bu sayede "null gelme" veya "eşleşmeme" sorunu ortadan kalkar.
            var files = Request.Form.Files;

            var folderPath = Path.Combine(_env.WebRootPath, "transfer");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            if (files == null || files.Count == 0)
            {
                // Dosya seçilmediyse veya limit sorunu varsa buraya düşer
                return RedirectToAction("Manage");
            }

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var filePath = Path.Combine(folderPath, file.FileName);

                    // Create modu: Varsa üzerine yazar, yoksa oluşturur.
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }

            return RedirectToAction("Manage");
        }

        [HttpPost]
        public IActionResult Delete(string fileName)
        {
            var folderPath = Path.Combine(_env.WebRootPath, "transfer");
            var filePath = Path.Combine(folderPath, fileName);

            if (System.IO.File.Exists(filePath) && filePath.StartsWith(folderPath))
            {
                System.IO.File.Delete(filePath);
            }
            return RedirectToAction("Manage");
        }

        // Yardımcı Metot (Kod tekrarını önlemek için)
        private List<TransferFileModel> GetFileList(string folderPath)
        {
            var fileList = new List<TransferFileModel>();
            var files = Directory.GetFiles(folderPath);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                fileList.Add(new TransferFileModel
                {
                    Name = fileInfo.Name,
                    Size = FormatSize(fileInfo.Length),
                    CreatedDate = fileInfo.CreationTime,
                    Extension = fileInfo.Extension.ToLower()
                });
            }
            return fileList;
        }

        private string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1) { number = number / 1024; counter++; }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

       
    }
    public class TransferFileModel
    {
        public string Name { get; set; }
        public string Size { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Extension { get; set; }
    }
}