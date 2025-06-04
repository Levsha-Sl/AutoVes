using System;
using System.Threading.Tasks;
using WeighingSystem.Services.Camera;

namespace WeighingSystem.Services
{
    public interface ICameraService
    {
        /// <summary>
        /// Асинхронно запускает видео трансляцию
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Асинхронно останавливает видео трансляцию
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Захватывает фото
        /// </summary>
        byte[] CapturePhoto();

        void UpdatePermission(double width, double height);

        event Action<byte[]> OnFrameReceived;
        void SubscribeToFrameReceived(Action<byte[]> onFrameReceived);
        void UnsubscribeFromFrameReceived(Action<byte[]> onFrameReceived);
    }

    /// <summary>
    /// Определяет какой сервис использовать для камеры
    /// </summary>
    public class CameraService : ICameraService
    {
        private readonly ICamera _camera;

        public CameraService(string connectionString) {
            _camera = new OpenCvCamera(connectionString);
        }

        public event Action<byte[]> OnFrameReceived;

        public void SubscribeToFrameReceived(Action<byte[]> onFrameReceived)
        {
            _camera.OnFrameReceived += onFrameReceived;
        }
        public void UnsubscribeFromFrameReceived(Action<byte[]> onFrameReceived)
        {
            _camera.OnFrameReceived -= onFrameReceived;
        }

        public async Task StartAsync()
        {
            await _camera.StartAsync();
        }

        public async Task StopAsync()
        {
            await _camera.StopAsync();
        }

        public byte[] CapturePhoto()
        {
            return _camera.CapturePhoto();
        }

        public void Dispose()
        {
            _camera.Dispose();
        }

        public void UpdatePermission(double width, double height)
        {
           _camera.UpdatePermission(width, height);
        }
    }
}
