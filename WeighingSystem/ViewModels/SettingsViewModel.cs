using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WeighingSystem.Commands;
using WeighingSystem.Models;
using WeighingSystem.Services;

namespace WeighingSystem.ViewModels
{
    public class SettingsViewModel : BasicModel
    {
        public ObservableCollection<CameraModel> Cameras { get; set; } = new ObservableCollection<CameraModel>();

        private CameraModel _selectedCamera = null;
        public CameraModel SelectedCamera
        {
            get { return _selectedCamera; }
            set
            {
                _selectedCamera = value;
                OnPropertyChanged();
            }
        }

        #region Обработка команд и событий для UI
        public ICommand UpdateCameras { get; }
        public ICommand CamerasRowEdit { get; }
        public ICommand AddCamera { get; }
        public ICommand DeleteCamera { get; }

        private void LoadCameras()
        {
            try
            {
                databaseFirstCode.LoadCameras(Cameras);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private void UpdateCamera(object SelectedItem)
        {
            try
            {
                if (SelectedItem is CameraModel camera)
                {
                    var countVideoCameras = 0;
                    var countPhotoCameras = 0;
                    foreach (var cameraModel in Cameras)
                    {
                        if (cameraModel.IsVideo)
                        {
                            countVideoCameras++;
                            if (countVideoCameras > 4)
                            {
                                camera.IsVideo = false;
                                throw new InvalidOperationException("Превышено количество камер для видео");
                            }
                        }
                        if (cameraModel.IsPhoto)
                        {
                            countPhotoCameras++;
                            if (countPhotoCameras > 8)
                            {
                                camera.IsPhoto = false;
                                throw new InvalidOperationException("Превышено количество камер для фото");
                            }
                        }
                    }

                    // Сохранение изменений в базе данных
                    if (!string.IsNullOrEmpty(camera.ConnectionString) && !string.IsNullOrEmpty(camera.Name))
                        databaseFirstCode.SaveCamera(camera);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Сообщение: {ex.Message}", "Ошибка");
            }
        }

        private void CreateCamera()
        {
            Cameras.Add(new CameraModel());
        }

        private void RemoveCamera()
        {
            if (SelectedCamera != null)
            {
                databaseFirstCode.RemoveCamera(SelectedCamera);
                Cameras.Remove(Cameras.FirstOrDefault(cameraModel => cameraModel.Id == SelectedCamera.Id));
                SelectedCamera = null;
            }
        }
        #endregion

        private DatabaseFirstCode databaseFirstCode;
        public SettingsViewModel()
        {
            databaseFirstCode = new DatabaseFirstCode();

            databaseFirstCode.LoadCameras(Cameras);

            #region привязка обработчиков к их командам
            UpdateCameras = new RelayCommand(LoadCameras);
            CamerasRowEdit = new RelayCommand(UpdateCamera);
            AddCamera = new RelayCommand(CreateCamera);
            DeleteCamera = new RelayCommand(RemoveCamera);
            #endregion
        }
    }
}
