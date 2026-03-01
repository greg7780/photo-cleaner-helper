using Microsoft.VisualBasic.FileIO;
using photo_cleaner_helper.Models;
using System.IO;

namespace photo_cleaner_helper.Services
{
    public class PhotoService
    {
        private readonly string _trashFolder;

        public PhotoService()
        {
            _trashFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PhotoCleanerTrash");

            if (!Directory.Exists(_trashFolder))
                Directory.CreateDirectory(_trashFolder);
        }

        public List<PhotoItem> Load(string folder)
        {
            return Directory.GetFiles(folder)
                .Where(f =>
                    f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".avi", StringComparison.OrdinalIgnoreCase))
                .Select(f => new PhotoItem
                {
                    FilePath = f,
                    CreatedAt = File.GetCreationTime(f)
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        public string MoveToTrash(string path)
        {
            var fileName = Path.GetFileName(path);
            var dest = Path.Combine(_trashFolder, fileName);

            File.Move(path, dest, true);
            return dest;
        }

        public void RestoreFromTrash(string trashPath, string originalPath)
        {
            File.Move(trashPath, originalPath, true);
        }
    }
}
