using OpenCvSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WeighingSystem.Services.Camera;

public class OpenCvCamera : ICamera
{
    private readonly string _rtspUrl;
    private VideoCapture _capture;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _videoTask;
    private bool IsConnected => _capture?.IsOpened() ?? false;

    private Size _targetSize;

    public event EventHandler<string> OnError;

    public event Action<byte[]> OnFrameReceived;

    public OpenCvCamera(string rtspUrl)
    {
        _rtspUrl = rtspUrl ?? throw new ArgumentNullException(nameof(rtspUrl));
    }

    public async Task StartAsync()
    {
        if (_capture != null) return;

        // Инициализация камеры в фоновом потоке
        await Task.Run(() => Connect());

        _cancellationTokenSource = new CancellationTokenSource();
        _videoTask = Task.Run(() => ProcessFramesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }

    private void Connect()
        {
        if (IsConnected)
            {
            HandleError("Camera is already connected.");
            return;
            }
        else
        {
            // Настройка VideoCapture для низкой задержки
            _capture = new VideoCapture(_rtspUrl, VideoCaptureAPIs.FFMPEG);
            _capture.Set(VideoCaptureProperties.BufferSize, 1); // Минимизируем буфер
            _capture.Set(VideoCaptureProperties.FourCC, FourCC.H264); // Устанавливаем H.264, если поддерживается
        }

        if (!IsConnected)
    {
                throw new InvalidOperationException("Не удалось открыть поток RTSP.");
            }

        // Устанавливаем начальный размер кадра
        //_targetSize = new Size(_capture.FrameWidth, _capture.FrameHeight);
    }

    public async Task StopAsync()
    {
        if (_cancellationTokenSource == null || _capture == null) return;

        _cancellationTokenSource.Cancel();

        try
        {
            // Ждём завершения задачи обработки кадров
            await _videoTask;
        }
        catch (OperationCanceledException)
        {
            // Ожидаемая ошибка при отмене токена
        }
        finally
        {
            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public byte[] CapturePhoto()
    {
        Connect();

        if (_capture == null)
        {
            throw new InvalidOperationException("Камера не запущена.");
        }

        using (var frame = new Mat())
        {
            _capture.Read(frame);

            if (frame.Empty())
            {
                throw new InvalidOperationException("Не удалось сделать снимок.");
            }

            return frame.ToBytes(".jpg");
        }
    }

    private async Task ProcessFramesAsync(CancellationToken cancellationToken)
    {
        using (var frame = new Mat())
        {
            using (var resizedFrame = new Mat())
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!_capture.Read(frame) || frame.Empty())
                    {
                        await Task.Delay(5, cancellationToken); // Минимальная пауза при ошибке
                        continue;
                    }

                    // Изменяем размер кадра
                    Cv2.Resize(frame, resizedFrame, _targetSize, interpolation: InterpolationFlags.Nearest);

                    var frameCopy = resizedFrame.Clone();
                    try
                    {
                        OnFrameReceived?.Invoke(MatToByteArray(frameCopy));
                    }
                    finally
                    {
                        frameCopy.Dispose();
                    }

                    // Логирование времени обработки

                    await Task.Delay(1, cancellationToken);
                }
            }
        }
    }

    public byte[] MatToByteArray(Mat mat)
    {
        using (var stream = new MemoryStream())
        {            
            // Кодируем изображение в формат jpg и записываем в поток
            Cv2.ImEncode(".jpg", mat, out var buffer, new int[] { (int)ImwriteFlags.JpegQuality, 50 });

            // Возвращаем массив байтов
            return buffer;
        }
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _capture?.Dispose();
    }

    private void HandleError(string message)
    {
        OnError?.Invoke(this, message); // Генерируем событие об ошибке
    }

    public void UpdatePermission(double width, double height)
    {
        _targetSize = new Size(width, height);
    }
}