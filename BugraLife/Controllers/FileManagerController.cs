using BugraLife.DBContext;
using BugraLife.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 

namespace BugraLife.Controllers
{
    [Authorize]
    public class FileManagerController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly BugraLifeDBContext _context; // Veritabanı eklendi

        public FileManagerController(IWebHostEnvironment env, BugraLifeDBContext context)
        {
            _env = env;
            _context = context;
        }

        // =========================================================================
        // 1. ANA SAYFA (LİSTELEME)
        // =========================================================================
        public IActionResult Index(string path = "")
        {
            if (string.IsNullOrEmpty(_env.WebRootPath)) return Content("Hata: wwwroot bulunamadı.");

            var rootPath = Path.Combine(_env.WebRootPath, "paylasim");
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

            var currentFullPath = Path.Combine(rootPath, path ?? "");

            // Güvenlik: Kök dizin dışına çıkılmasın
            if (!Path.GetFullPath(currentFullPath).StartsWith(rootPath))
            {
                currentFullPath = rootPath;
                path = "";
            }

            if (!Directory.Exists(currentFullPath))
            {
                currentFullPath = rootPath;
                path = "";
            }

            var model = new FileManagerViewModel
            {
                CurrentPath = path,
                ParentPath = string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path)?.Replace("\\", "/")
            };

            try
            {
                // Klasörler
                var dirs = Directory.GetDirectories(currentFullPath);
                foreach (var dir in dirs)
                {
                    var dirName = new DirectoryInfo(dir).Name;
                    model.Directories.Add(new FileItem
                    {
                        Name = dirName,
                        Path = Path.Combine(path ?? "", dirName).Replace("\\", "/"),
                        IsDirectory = true,
                        Icon = "bi-folder-fill text-warning"
                    });
                }

                // Dosyalar
                var files = Directory.GetFiles(currentFullPath);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    model.Files.Add(new FileItem
                    {
                        Name = fileInfo.Name,
                        Path = Path.Combine(path ?? "", fileInfo.Name).Replace("\\", "/"),
                        IsDirectory = false,
                        Size = FormatSize(fileInfo.Length),
                        Extension = fileInfo.Extension.ToLower(),
                        Icon = GetFileIcon(fileInfo.Extension)
                    });
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Hata: " + ex.Message;
            }

            return View(model);
        }

        // =========================================================================
        // 2. YENİ KLASÖR OLUŞTUR
        // =========================================================================
        [HttpPost]
        public IActionResult CreateFolder(string currentPath, string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName)) return RedirectToAction("Index", new { path = currentPath });

            var rootPath = Path.Combine(_env.WebRootPath, "paylasim");
            var fullPath = Path.Combine(rootPath, currentPath ?? "", folderName);

            if (Path.GetFullPath(fullPath).StartsWith(rootPath) && !Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            return RedirectToAction("Index", new { path = currentPath });
        }

        // =========================================================================
        // 3. PARÇALI DOSYA YÜKLEME (CHUNK UPLOAD - SINIRSIZ)
        // =========================================================================
        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> UploadChunk(string currentPath, IFormFile chunk, string fileName, int chunkIndex, int totalChunks)
        {
            try
            {
                var rootPath = Path.Combine(_env.WebRootPath, "paylasim");
                var targetFolder = Path.Combine(rootPath, currentPath ?? "");

                if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                // Güvenlik
                if (!Path.GetFullPath(targetFolder).StartsWith(rootPath))
                    return Json(new { success = false, message = "Geçersiz yol." });

                var filePath = Path.Combine(targetFolder, fileName);

                // İlk parçaysa eski dosyayı sil (Sıfırdan yazıyoruz)
                if (chunkIndex == 0 && System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Parçayı dosyanın ucuna ekle (Append)
                using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    await chunk.CopyToAsync(stream);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =========================================================================
        // 4. SİLME İŞLEMİ
        // =========================================================================
        [HttpPost]
        public IActionResult Delete(string path)
        {
            if (string.IsNullOrEmpty(path)) return RedirectToAction("Index");

            var rootPath = Path.Combine(_env.WebRootPath, "paylasim");
            var fullPath = Path.Combine(rootPath, path);

            if (Path.GetFullPath(fullPath).StartsWith(rootPath))
            {
                try
                {
                    if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                    else if (Directory.Exists(fullPath)) Directory.Delete(fullPath, true);
                }
                catch { }
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            return RedirectToAction("Index", new { path = parent });
        }

        // =========================================================================
        // 5. YENİDEN ADLANDIRMA (RENAME)
        // =========================================================================
        [HttpPost]
        public IActionResult RenameItem(string currentPath, string oldPath, string newName)
        {
            try
            {
                var rootPath = Path.Combine(_env.WebRootPath, "paylasim");
                var fullOldPath = Path.Combine(rootPath, oldPath);

                if (!Path.GetFullPath(fullOldPath).StartsWith(rootPath)) return Json(new { success = false, message = "Geçersiz işlem." });

                bool isDirectory = Directory.Exists(fullOldPath);
                string parentDir = Path.GetDirectoryName(fullOldPath);
                string fullNewPath;

                if (!isDirectory)
                {
                    string ext = Path.GetExtension(fullOldPath);
                    // Kullanıcı uzantıyı sildiyse biz ekleyelim
                    if (!newName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) newName += ext;
                    fullNewPath = Path.Combine(parentDir, newName);
                }
                else
                {
                    fullNewPath = Path.Combine(parentDir, newName);
                }

                if (fullOldPath != fullNewPath)
                {
                    if (isDirectory) Directory.Move(fullOldPath, fullNewPath);
                    else System.IO.File.Move(fullOldPath, fullNewPath);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =========================================================================
        // 6. TAŞIMA (MOVE) VE KLASÖR LİSTESİ
        // =========================================================================
        [HttpGet]
        public IActionResult GetAllFolders()
        {
            var rootPath = Path.Combine(_env.WebRootPath, "paylasim");
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

            var folderList = new List<object>();
            folderList.Add(new { path = "", name = "Ana Dizin (/)" });
            GetDirectoriesRecursive(rootPath, rootPath, folderList);
            return Json(folderList);
        }

        private void GetDirectoriesRecursive(string rootPath, string currentPath, List<object> list)
        {
            try
            {
                var dirs = Directory.GetDirectories(currentPath);
                foreach (var dir in dirs)
                {
                    string relativePath = Path.GetRelativePath(rootPath, dir).Replace("\\", "/");
                    string name = new DirectoryInfo(dir).Name;
                    int depth = relativePath.Split('/').Length;
                    string displayName = new string('-', depth) + " " + name;
                    list.Add(new { path = relativePath, name = displayName });
                    GetDirectoriesRecursive(rootPath, dir, list);
                }
            }
            catch { }
        }

        [HttpPost]
        public IActionResult MoveItem(string itemPath, string targetFolderPath)
        {
            try
            {
                var rootPath = Path.Combine(_env.WebRootPath, "paylasim");
                var sourcePath = Path.Combine(rootPath, itemPath);
                var destPath = Path.Combine(rootPath, targetFolderPath ?? "");

                if (!Path.GetFullPath(sourcePath).StartsWith(rootPath) || !Path.GetFullPath(destPath).StartsWith(rootPath))
                    return Json(new { success = false, message = "Geçersiz yol." });

                if (!Directory.Exists(destPath)) return Json(new { success = false, message = "Hedef klasör yok." });

                string itemName = Path.GetFileName(sourcePath);
                string finalDestPath = Path.Combine(destPath, itemName);

                if (sourcePath == finalDestPath) return Json(new { success = false, message = "Dosya zaten burada." });

                if (Directory.Exists(sourcePath))
                {
                    if (finalDestPath.StartsWith(sourcePath)) return Json(new { success = false, message = "Klasör kendi içine taşınamaz." });
                    Directory.Move(sourcePath, finalDestPath);
                }
                else if (System.IO.File.Exists(sourcePath))
                {
                    System.IO.File.Move(sourcePath, finalDestPath);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =========================================================================
        // 7. İNDİRME VE ÖNİZLEME
        // =========================================================================
        [HttpGet]
        public IActionResult Download(string path, bool isInline = false)
        {
            if (string.IsNullOrEmpty(path)) return RedirectToAction("Index");

            var rootPath = Path.Combine(_env.WebRootPath, "paylasim");
            var fullPath = Path.Combine(rootPath, path);

            if (Path.GetFullPath(fullPath).StartsWith(rootPath) && System.IO.File.Exists(fullPath))
            {
                var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(fullPath, out string contentType)) contentType = "application/octet-stream";

                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

                if (isInline) return File(fileStream, contentType); // Tarayıcıda aç
                return File(fileStream, contentType, Path.GetFileName(fullPath)); // İndir
            }

            return RedirectToAction("Index");
        }

        // YARDIMCILAR
        private string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1) { number = number / 1024; counter++; }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

        private string GetFileIcon(string ext)
        {
            return ext.ToLower() switch
            {
                ".pdf" => "bi-file-pdf-fill text-danger",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => "bi-file-image-fill text-info",
                ".zip" or ".rar" or ".7z" => "bi-file-zip-fill text-warning",
                ".txt" => "bi-file-text-fill text-secondary",
                ".xlsx" or ".xls" => "bi-file-excel-fill text-success",
                ".docx" or ".doc" => "bi-file-word-fill text-primary",
                ".mp4" or ".avi" or ".mov" => "bi-file-play-fill text-danger",
                ".mp3" or ".wav" => "bi-file-music-fill text-success",
                _ => "bi-file-earmark-fill text-light"
            };
        }


        [HttpPost]
        public async Task<IActionResult> CreateShareLink(string currentPath, string fileName)
        {
            try
            {
                var relativePath = Path.Combine(currentPath ?? "", fileName).Replace("\\", "/");

                // Zaten aktif bir paylaşım var mı? Varsa onu döndür.
                var existingShare = await _context.FileShareds
                    .FirstOrDefaultAsync(x => x.FilePath == relativePath && x.IsActive);

                string token;
                if (existingShare != null)
                {
                    token = existingShare.Token;
                }
                else
                {
                    token = Guid.NewGuid().ToString("N"); // Rastgele temiz bir kod
                    var newShare = new FileShared
                    {
                        Token = token,
                        FilePath = relativePath,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    _context.FileShareds.Add(newShare);
                    await _context.SaveChangesAsync();
                }

                // Linki oluştur: https://siteadi.com/s/TOKEN
                var shareUrl = Url.Action("PublicDownload", "FileManager", new { token = token }, Request.Scheme);

                // İstersen rota özelleştirip /s/token yapabilirsin ama şimdilik standart route:
                // /FileManager/PublicDownload?token=...

                return Json(new { success = true, url = shareUrl, token = token });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // B. PAYLAŞIMI İPTAL ET (REVOKE)
        [HttpPost]
        public async Task<IActionResult> RevokeShare(string token)
        {
            var share = await _context.FileShareds.FirstOrDefaultAsync(x => x.Token == token);
            if (share != null)
            {
                share.IsActive = false; // Pasife çek
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Paylaşım bulunamadı." });
        }

        // C. GENEL İNDİRME (LOGIN GEREKTİRMEZ!)
        [AllowAnonymous] // <--- KRİTİK NOKTA: Herkese açık
        [HttpGet]
        public async Task<IActionResult> PublicDownload(string token)
        {
            if (string.IsNullOrEmpty(token)) return Content("Geçersiz Link");

            var share = await _context.FileShareds.FirstOrDefaultAsync(x => x.Token == token && x.IsActive);

            if (share == null) return Content("Bu paylaşım linki iptal edilmiş veya geçersiz.");

            // Dosya yolunu bul
            var rootPath = Path.Combine(_env.WebRootPath, "paylasim");
            var fullPath = Path.Combine(rootPath, share.FilePath);

            if (System.IO.File.Exists(fullPath))
            {
                var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(fullPath, out string contentType)) contentType = "application/octet-stream";

                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                return File(fileStream, contentType, Path.GetFileName(fullPath));
            }

            return Content("Dosya sunucudan silinmiş.");
        }
    }
}