using photo_cleaner_helper.Models;
using photo_cleaner_helper.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Shell;
using System.Windows.Interop;

namespace photo_cleaner_helper.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly PhotoService _photoService = new();

        public ObservableCollection<PhotoItem> Photos { get; } = new();

        private int _currentIndex = 0;

        private string? _lastDeletedOriginalPath;
        private string? _lastDeletedTrashPath;

        private readonly Dictionary<string, BitmapImage> _imageCache = new();

        public string? CurrentPath => 
            Photos.Any() ? Photos[_currentIndex].FilePath : null;

        public string? CurrentVideo => 
            IsVideo ? Photos[_currentIndex].FilePath : null;

        public bool IsImage =>
            Photos.Any() && !IsVideo;

        public bool IsVideo =>
            Photos.Any() &&
            (Photos[_currentIndex].FilePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
             Photos[_currentIndex].FilePath.EndsWith(".mov", StringComparison.OrdinalIgnoreCase) ||
             Photos[_currentIndex].FilePath.EndsWith(".avi", StringComparison.OrdinalIgnoreCase));

        public Visibility IsImageVisible =>
            (!IsVideoFile(CurrentPath) || !IsPlaying)
                ? Visibility.Visible
                : Visibility.Collapsed;

        public Visibility IsVideoVisible =>
            (IsVideoFile(CurrentPath) && IsPlaying)
                ? Visibility.Visible
                : Visibility.Collapsed;

        public bool IsCurrentVideo => 
            IsVideoFile(CurrentPath);

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                Notify(nameof(IsPlaying));
                Notify(nameof(IsImageVisible));
                Notify(nameof(IsVideoVisible));
            }
        }

        public BitmapSource? CurrentImage
        {
            get
            {
                if (!Photos.Any())
                    return null;

                var path = Photos[_currentIndex].FilePath;

                if (IsVideoFile(path))
                    return LoadVideoThumbnail(path);

                if (_imageCache.TryGetValue(path, out var cached))
                    return cached;

                var bitmap = LoadBitmap(path);
                _imageCache[path] = bitmap;

                PreloadNeighbors();

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
            Notify(nameof(CurrentVideo));
            Notify(nameof(CurrentPath));
            Notify(nameof(IsImageVisible));
            Notify(nameof(IsVideoVisible));
            Notify(nameof(IsCurrentVideo));
        }

        public void Next()
        {
            if (_currentIndex < Photos.Count - 1)
            {
                _currentIndex++;
                Notify(nameof(CurrentImage));
                Notify(nameof(CurrentVideo));
                Notify(nameof(CurrentPath));
                Notify(nameof(IsImageVisible));
                Notify(nameof(IsVideoVisible));
                Notify(nameof(IsCurrentVideo));
            }
        }

        public void Previous()
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                Notify(nameof(CurrentImage));
                Notify(nameof(CurrentVideo));
                Notify(nameof(CurrentPath));
                Notify(nameof(IsImageVisible));
                Notify(nameof(IsVideoVisible));
                Notify(nameof(IsCurrentVideo));
            }
        }

        public void DeleteCurrent()
        {
            if (!Photos.Any()) return;

            var photo = Photos[_currentIndex];

            _lastDeletedOriginalPath = photo.FilePath;
            _lastDeletedTrashPath = _photoService.MoveToTrash(photo.FilePath);

            Photos.RemoveAt(_currentIndex);

            if (_currentIndex >= Photos.Count)
                _currentIndex = Photos.Count - 1;

            Notify(nameof(CurrentImage));
            Notify(nameof(CurrentVideo));
            Notify(nameof(CurrentPath));
            Notify(nameof(IsImageVisible));
            Notify(nameof(IsVideoVisible));
            Notify(nameof(IsCurrentVideo));

            _imageCache.Remove(photo.FilePath);
        }

        public void UndoDelete()
        {
            if (_lastDeletedOriginalPath == null || _lastDeletedTrashPath == null)
                return;

            _photoService.RestoreFromTrash(
                _lastDeletedTrashPath,
                _lastDeletedOriginalPath);

            Photos.Insert(_currentIndex, new PhotoItem
            {
                FilePath = _lastDeletedOriginalPath,
                CreatedAt = File.GetCreationTime(_lastDeletedOriginalPath)
            });

            _lastDeletedOriginalPath = null;
            _lastDeletedTrashPath = null;

            Notify(nameof(CurrentImage));
            Notify(nameof(CurrentVideo));
            Notify(nameof(CurrentPath));
            Notify(nameof(IsImageVisible));
            Notify(nameof(IsVideoVisible));
            Notify(nameof(IsCurrentVideo));
        }

        private BitmapSource LoadVideoThumbnail(string path)
        {
            using var shellFile = ShellFile.FromFilePath(path);
            var bitmap = shellFile.Thumbnail.LargeBitmap;

            var hBitmap = bitmap.GetHbitmap();

            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return source;
        }

        private bool IsVideoFile(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".avi", StringComparison.OrdinalIgnoreCase);
        }

        private BitmapImage LoadBitmap(string path)
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 1920;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        private void PreloadNeighbors()
        {
            Preload(_currentIndex - 1);
            Preload(_currentIndex + 1);
        }

        private void Preload(int index)
        {
            if (index < 0 || index >= Photos.Count)
                return;

            var path = Photos[index].FilePath;

            if (IsVideoFile(path))
                return;

            if (_imageCache.ContainsKey(path))
                return;

            _imageCache[path] = LoadBitmap(path);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Notify([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
