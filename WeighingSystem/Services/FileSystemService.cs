using System.IO;

namespace WeighingSystem.Services
{
    public interface IFileSystemService
    {
        /// <summary>
        /// Сохраняет фото в указанную директорию.
        /// </summary>
        string SavePhoto(byte[] imageData, string directoryPath, string fileName);

        /// <summary>
        /// Создает директорию, если она не существует.
        /// </summary>
        void EnsureDirectoryExists(string directoryPath);
    }

    public class FileSystemService : IFileSystemService
    {
        public string SavePhoto(byte[] imageData, string directoryPath, string fileName)
        {
            EnsureDirectoryExists(directoryPath);
            var fullPath = Path.Combine(directoryPath, fileName);
            File.WriteAllBytes(fullPath, imageData);
            return fullPath;
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
    }
}
