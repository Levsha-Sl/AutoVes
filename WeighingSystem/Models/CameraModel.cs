using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using WeighingSystem.Helpers;
using WeighingSystem.Services;

namespace WeighingSystem.Models
{
    public class CameraModel : BasicModel
    {
        private int _id;
        public int Id
        {
            get { return _id; }
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
        private string _connectionString;
        public string ConnectionString {
            get { return _connectionString; } 
            set
            {
                _connectionString = value;
                OnPropertyChanged();
            } 
        }
        private bool _isVideo;
        public bool IsVideo
        {
            get { return _isVideo; }
            set
            {
                _isVideo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAvailableForActivation));
            }
        }

        private bool _isPhoto;
        public bool IsPhoto
        {
            get { return _isPhoto; }
            set
            {
                _isPhoto = value;
                OnPropertyChanged();
            }
        }

        private bool _isCapturingPhoto;
        public bool IsCapturingPhoto
        {
            get { return _isCapturingPhoto; }
            private set
            {
                _isCapturingPhoto = value;
                OnPropertyChanged();
            }
        }

        private bool _isStreamingVideo;
        public bool IsStreamingVideo
        {
            get { return _isStreamingVideo; }
            private set
            {
                _isStreamingVideo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsVideoInRootMenu));
            }
        }

        private bool _isVideoInRootMenu;
        public bool IsVideoInRootMenu
        {
            get { return _isVideoInRootMenu; }
            set
            {
                _isVideoInRootMenu = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAvailableForActivation));
            }
        }

        public bool IsAvailableForActivation => IsVideo && !IsVideoInRootMenu;

        private BitmapImage _image = new BitmapImage();
        public BitmapImage Image
        {
            get { return _image; }
            private set
            {
                _image = value;
                OnPropertyChanged();
            }
        }

        private Size _permission = new Size(320, 240); // разрешение 360р
        public Size Permission
        {
            get { return _permission; }
            set
            {
                _permission = ResolutionHelper.GetClosestResolution(value);
                OnPropertyChanged();
            }
        }

        private readonly ICameraService _cameraService;

        public CameraModel(int id = -1, string name = "", string connectionString = "", bool isVideo = false, bool isPhoto = false)
        {
            Id = id;
            Name = name;
            ConnectionString = connectionString;
            IsVideo = isVideo;
            IsPhoto = isPhoto;

            _cameraService = new CameraService(ConnectionString);
            _cameraService.UpdatePermission(Permission.Width, Permission.Height);
        }

        public async Task InitializationAsync()
        {
            if (IsVideo)
                await StartAsync();
        }

        public async Task StartAsync()
        {
            if (!IsVideo || IsStreamingVideo)
                return;

            IsStreamingVideo = true;
            try
            {
                await _cameraService.StartAsync();
                // Подписываемся на PropertyChanged здесь
                PropertyChanged += OnCameraPropertyChanged;

                // Подписываемся на событие получения кадров
                _cameraService.SubscribeToFrameReceived(OnFrameReceived);
            }
            catch
            {
                IsStreamingVideo = false;
                IsVideo = false;
                throw;
            }
        }

        public async void StopAsync()
        {
            if (!IsStreamingVideo) return;

            try
            {
                _cameraService.UnsubscribeFromFrameReceived(OnFrameReceived);
                PropertyChanged -= OnCameraPropertyChanged;
                await _cameraService.StopAsync();
            }
            finally
            {
                IsStreamingVideo = false;
            }
        }

        private void OnCameraPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Permission))
            {
                _cameraService.UpdatePermission(Permission.Width,Permission.Height);
            }
        }

        private void OnFrameReceived(byte[] frameBytes)
        {
            // Обновляем свойство Image в UI потоке
            Application.Current.Dispatcher.Invoke(() =>
            {
                Image = ByteArrayToBitmapImage(frameBytes);
            });
        }

        public byte[] CapturePhoto()
        {
            if (IsPhoto)
            {
                if (IsCapturingPhoto) return new byte[0];

                IsCapturingPhoto = true;
                try
                {
                    return _cameraService.CapturePhoto();
                }
                finally
                {
                    IsCapturingPhoto = false;
                }
            }
            else
            {
                return new byte[0];
            }
        }

        
        private BitmapImage ByteArrayToBitmapImage(byte[] byteArray)
        {
            var image = new BitmapImage();
            using (var stream = new MemoryStream(byteArray))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
            }
            return image;
        }
    }
}