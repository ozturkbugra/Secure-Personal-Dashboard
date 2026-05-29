namespace BugraLife.Models
{
    public class FileManagerViewModel
    {
        public string CurrentPath { get; set; } // Şu an hangi klasördeyiz? (Örn: "Tatil/Resimler")
        public string ParentPath { get; set; }  // Bir üst klasör ne? (Geri tuşu için)

        public List<FileItem> Directories { get; set; } = new List<FileItem>();
        public List<FileItem> Files { get; set; } = new List<FileItem>();
    }

    public class FileItem
    {
        public string Name { get; set; }
        public string Path { get; set; } // Göreceli yol
        public bool IsDirectory { get; set; }
        public string Size { get; set; } // Dosya boyutu (1.2 MB)
        public string Extension { get; set; } // .jpg, .pdf vs.
        public string Icon { get; set; } // Bootstrap icon class
    }
}