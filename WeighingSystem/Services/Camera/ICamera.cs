using System;
using System.Threading.Tasks;

namespace WeighingSystem.Services.Camera
{
    public interface ICamera: IDisposable
    {
        /// <summary>
        /// Запускает камеру.
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Останавливает камеру.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Захватывает текущий кадр с камеры.
        /// </summary>
        /// <returns>Массив байтов изображения.</returns>
        byte[] CapturePhoto();
       
        void UpdatePermission(double width, double height);

        /// <summary>
        /// Событие для трансляции видео
        /// </summary>
        /// <returns>Видеопоток в формате массива байтов.</returns>
        event Action<byte[]> OnFrameReceived;
    }
}
