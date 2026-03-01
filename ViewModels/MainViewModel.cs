using photo_cleaner_helper.Models;
using photo_cleaner_helper.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace photo_cleaner_helper.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly PhotoService _photoService = new();

        public ObservableCollection<PhotoItem> Photos { get; } = new();

        private int _currentIndex = 0;
        private PhotoItem? _lastDeleted;

        public BitmapImage? CurrentImage
        {
            get
            {
                if (!Photos.Any()) return null;

                var path = Photos[_currentIndex].FilePath;

                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
        }

        public void LoadFolder(string folder)
        {
            Photos.Clear();
            foreach (var photo in _photoService.Load(folder))
                Photos.Add(photo);

            _currentIndex = 0;
            Notify(nameof(CurrentImage));
        }

        public void Next()
        {
            if (_currentIndex < Photos.Count - 1)
            {
                _currentIndex++;
                Notify(nameof(CurrentImage));
            }
        }

        public void Previous()
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                Notify(nameof(CurrentImage));
            }
        }

        public void DeleteCurrent()
        {
            if (!Photos.Any()) return;

            var photo = Photos[_currentIndex];
            _photoService.DeleteToRecycleBin(photo.FilePath);

            _lastDeleted = photo;
            Photos.RemoveAt(_currentIndex);

            if (_currentIndex >= Photos.Count)
                _currentIndex = Photos.Count - 1;

            Notify(nameof(CurrentImage));
        }

        public void UndoDelete()
        {
            if (_lastDeleted == null) return;

            Photos.Insert(_currentIndex, _lastDeleted);
            _lastDeleted = null;

            Notify(nameof(CurrentImage));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Notify([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
