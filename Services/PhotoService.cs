using Microsoft.VisualBasic.FileIO;
using photo_cleaner_helper.Models;
using System.IO;

namespace photo_cleaner_helper.Services
{
    public class PhotoService
    {
        public List<PhotoItem> Load(string folder)
        {
            return Directory.GetFiles(folder)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .Select(f => new PhotoItem
                {
                    FilePath = f,
                    CreatedAt = File.GetCreationTime(f)
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        public void DeleteToRecycleBin(string path)
        {
            FileSystem.DeleteFile(
                path,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin
            );
        }
    }
}
