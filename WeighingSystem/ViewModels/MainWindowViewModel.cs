using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WeighingSystem.Commands;
using WeighingSystem.Models;
using WeighingSystem.Services;
using WeighingSystem.Views;

namespace WeighingSystem.ViewModels
{
    public class MainWindowViewModel : BasicModel
    {
        private int? _currentWeight = null;
        public int? CurrentWeight
        {
            get { return _currentWeight; }
            private set
            {
                if (_currentWeight != value)
                {
                    _currentWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _weighingPhotoSource;
        public string WeighingPhotoSource
        {
            get { return _weighingPhotoSource; }
            private set
            {
                _weighingPhotoSource = value;
                OnPropertyChanged();
                OnPropertyChanged(() => PhotoType);
            }
        }

        public string PhotoType
        {
            get
            {
                if (WeighingPhotoSource.Contains(nameof(SelectedWeighing.GrossTime)))
                {
                    return "брутто";
                }
                else if (WeighingPhotoSource.Contains(nameof(SelectedWeighing.TareTime)))
                {
                    return "тара";
                }
                else
                    return "Unknown"; // Если ни одна из подстрок не найдена
            }
        }

        private Weighing _selectedWeighing = new Weighing();
        public Weighing SelectedWeighing
        {
            get { return _selectedWeighing; }
            private set
            {
                _selectedWeighing = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<CameraModel> Cameras { get; set; } = new ObservableCollection<CameraModel>();
        public List<CameraModel> AvailableCameras => Cameras?.Where(camera => camera.IsVideo && !camera.IsVideoInRootMenu).ToList();

        public ObservableCollection<string> ListVehicle { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ListCargoType { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ListSourceWarehouse { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ListDestinationWarehouse { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ListCounterparty { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> ListDriver { get; set; } = new ObservableCollection<string>();
       
        //древо
        private ObservableCollection<Location> _listLocations = new ObservableCollection<Location>();
        public ObservableCollection<Location> ListLocations
        {
            get => _listLocations;
            set
            {
                if (_listLocations != value)
                {
                    _listLocations = value;
                    OnPropertyChanged(nameof(ListLocations)); // Уведомляем интерфейс о замене коллекции
                }
            }
        }

        private readonly IFileSystemService fileSystemService;
        private readonly ScaleService scaleService;

        #region Обработка команд и событий для UI
        public ICommand ResetWeight { get; }
        public ICommand NewWeigh { get; }

        public ICommand WriteTareWeight { get; }
        public ICommand WriteGrossWeight { get; }
        public ICommand WriteWeight { get; }

        public ICommand PreviousPhoto { get; }
        public ICommand NextPhoto { get; }

        public ICommand OpenVehicleReference { get; }
        public ICommand OpenCargoTypeReference { get; }
        public ICommand OpenSourceWarehouseReference { get; }
        public ICommand OpenDestinationWarehouseReference { get; }
        public ICommand OpenCounterpartyReference { get; }
        public ICommand OpenDriverReference { get; }

        public ICommand GeneratePdf { get; }
        public ICommand GenerateExcel { get; }
        public ICommand OpenSettings { get; }
        public ICommand OpenHistory { get; }
        public ICommand OpenHelp { get; }

        public ICommand WeighingTree_SelectedItemChanged { get; }

        public ICommand WindowClosingCommand { get; }

        public ICommand ShowPreviewCommand { get; }
        public ICommand ClosingCameraCommand { get; }
        public ICommand ActivateCameraCommand { get; }

        private void SetWeightZero()
        {
            if (scaleService.isConnect)
            {
                scaleService.SetZero();
            }
            else
            {
                scaleService.Reload();
            }
            //CurrentWeight = 0;
        }

        private void ResetWeighData()
        {
            LoadHandbooks();

            SelectedWeighing.TareWeight = 0;
            SelectedWeighing.GrossWeight = 0;
            //NetWeight не задается
            SelectedWeighing.Vehicle = "";
            SelectedWeighing.CargoType = "";
            SelectedWeighing.SourceWarehouse = "";
            SelectedWeighing.DestinationWarehouse = "";
            SelectedWeighing.Counterparty = "";
            SelectedWeighing.Driver = "";
            SelectedWeighing.PathPhotos.Clear();
            SelectedWeighing.Id = -1;
            SelectedWeighing.PhotoIndex = -1;

            //TODO надо во вью стандарт делать
            WeighingPhotoSource = "";
        }

        private void SaveTareWeight()
        {
            SelectedWeighing.TareWeight = CurrentWeight?? 0;
            SelectedWeighing.TareTime = DateTime.Now;
            databaseFirstCode.SaveCurrentWeighing(SelectedWeighing,tareTime: $"{SelectedWeighing.TareTime:yyyy-MM-dd HH:mm:ss}");

            MessageBox.Show("Тара сохранена", "Успех");

            TakePhoto_Def(nameof(SelectedWeighing.TareTime));

            databaseFirstCode.LoadWeighingTree(ListLocations);
        }
        private void SaveGrossWeight()
        {
            SelectedWeighing.GrossWeight = CurrentWeight?? 0;
            SelectedWeighing.GrossTime = DateTime.Now;
            databaseFirstCode.SaveCurrentWeighing(SelectedWeighing, grossTime: $"{SelectedWeighing.GrossTime:yyyy-MM-dd HH:mm:ss}");

            MessageBox.Show("Брутто сохранено", "Успех");

            TakePhoto_Def(nameof(SelectedWeighing.GrossTime));

            databaseFirstCode.LoadWeighingTree(ListLocations);
        }
        
        
        private void SaveWeighing()
        {
            databaseFirstCode.SaveCurrentWeighing(SelectedWeighing);

            MessageBox.Show("Взвешивание сохранено", "Успех");
            ResetWeighData();
            databaseFirstCode.LoadWeighingTree(ListLocations);
        }

        private void GoPreviousPhoto()
        {
            if (SelectedWeighing.PhotoIndex > 0)
            {
                SelectedWeighing.PhotoIndex--;
                WeighingPhotoSource = SelectedWeighing.PathPhotos[SelectedWeighing.PhotoIndex];
            }
        }
        private void GoNextPhoto()
        {
            if (SelectedWeighing.PhotoIndex < SelectedWeighing.PathPhotos.Count - 1)
            {
                SelectedWeighing.PhotoIndex++;
                WeighingPhotoSource = SelectedWeighing.PathPhotos[SelectedWeighing.PhotoIndex];
            }
        }

        #region Обработчики кнопок справочников
        private void ShowVehicleReference()
        {
            var window = new ReferenceWindow("Vehicles", "license_plate", "Справочник автомобилей");
            window.Closed += (s, args) => databaseFirstCode.LoadHandbook("Vehicles", ListVehicle);
            window.ShowDialog();
        }
        private void ShowCargoTypeReference()
        {
            var window = new ReferenceWindow("CargoTypes", "name", "Справочник типов грузов");
            window.Closed += (s, args) => databaseFirstCode.LoadHandbook("CargoTypes", ListCargoType);
            window.ShowDialog();
        }
        private void ShowSourceWarehouseReference()
        {
            var window = new ReferenceWindow("Warehouses", "name", "Справочник складов");
            window.Closed += (s, args) => databaseFirstCode.LoadHandbook("Warehouses", ListSourceWarehouse);
            window.ShowDialog();
        }
        private void ShowDestinationWarehouseReference()
        {
            var window = new ReferenceWindow("Warehouses", "name", "Справочник складов");
            window.Closed += (s, args) => databaseFirstCode.LoadHandbook("Warehouses", ListDestinationWarehouse);
            window.ShowDialog();
        }
        private void ShowCounterpartyReference()
        {
            var window = new ReferenceWindow("Counterparties", "name", "Справочник контрагентов");
            window.Closed += (s, args) => databaseFirstCode.LoadHandbook("Counterparties", ListCounterparty);
            window.ShowDialog();
        }
        private void ShowDriverReference()
        {
            var window = new ReferenceWindow("Drivers", "full_name", "Справочник водителей");
            window.Closed += (s, args) => databaseFirstCode.LoadHandbook("Drivers", ListDriver);
            window.ShowDialog();
        }
        #endregion

        private void ExportToPdf() => MessageBox.Show("PDF отчет сгенерирован (заглушка)", "Отчет");
        private void ExportToExcel() => MessageBox.Show("Excel отчет сгенерирован (заглушка)", "Отчет");
        private void ShowSettings()
        {
            foreach (var camera in Cameras)
            {
                camera.StopAsync();
                camera.IsVideoInRootMenu = false;
            }
            new SettingsWindow().ShowDialog();
            databaseFirstCode.LoadCameras(Cameras);
            InitializeCamerasAsync();
        }
        private void ShowHistory() => new HistoryWindow().ShowDialog();
        private void ShowHelp() => new HelpWindow().ShowDialog();

        private void FindWeighting(object SelectedItem)
        {
            if (SelectedItem is Weighing weighing)
            {
                SelectedWeighing = databaseFirstCode.FindWeightingById(weighing.Id);

                SelectedWeighing.PhotoIndex = SelectedWeighing.PathPhotos.Count - 1;
                if (SelectedWeighing.PhotoIndex > 0)
                {
                    WeighingPhotoSource = SelectedWeighing.PathPhotos[SelectedWeighing.PhotoIndex];
                }
                // не обязательно.. в дереве уже лежит Weighing, но он не полный
                // SelectedWeighing = databaseFirstCode.FindWeightingById(weighing.Id);
            }
            else
            {
                // тут может быть логика других типов, например Location
            }
        }

        private void OnWindowClosing()
        {
            // Остановка опроса весов при закрытии окна
            scaleService.StopPolling();
            foreach (var camera in Cameras)
            {
                camera.StopAsync();
                camera.IsVideoInRootMenu = false;
            }
        }

        private void OpenCameraPreview(object param)
        {
            if (param is CameraModel selectedCamera)
            {
                // Открываем окно превью
                var previewWindow = new CameraPreviewWindow(selectedCamera);
                selectedCamera.IsVideoInRootMenu = false;
                previewWindow.Show();
            }
        }

        private void CloseCameraVideoStream(object param)
        {
            if (param is CameraModel selectedCamera)
            {
                selectedCamera.StopAsync();
                selectedCamera.IsVideoInRootMenu = false;
            }
        }

        private void OpenCameraVideoStream(object param)
        {
            if (param is CameraModel selectedCamera)
            {
                OpenCamerasAsync(selectedCamera);
            }
        }
        #endregion

        private DatabaseFirstCode databaseFirstCode;
        public MainWindowViewModel()
        {
            #region загрузка данных
            databaseFirstCode = new DatabaseFirstCode();
            // надо загрузить справочники
            LoadHandbooks();

            // надо загрузить древо взвешиваний
            databaseFirstCode.LoadWeighingTree(ListLocations);
            #endregion

            databaseFirstCode.LoadCameras(Cameras); // сервис камеры инициализируются при создании камеры
            
            #region подключение другим сервисам
            fileSystemService = new FileSystemService();
            scaleService = new ScaleService();
            #endregion

            #region привязка обработчиков к их командам
            ResetWeight = new RelayCommand(SetWeightZero);
            NewWeigh = new RelayCommand(ResetWeighData);

            WriteTareWeight = new RelayCommand(SaveTareWeight);
            WriteGrossWeight = new RelayCommand(SaveGrossWeight);
            WriteWeight = new RelayCommand(SaveWeighing);

            PreviousPhoto = new RelayCommand(GoPreviousPhoto);
            NextPhoto = new RelayCommand(GoNextPhoto);

            OpenVehicleReference = new RelayCommand(ShowVehicleReference);
            OpenCargoTypeReference = new RelayCommand(ShowCargoTypeReference);
            OpenSourceWarehouseReference = new RelayCommand(ShowSourceWarehouseReference);
            OpenDestinationWarehouseReference = new RelayCommand(ShowDestinationWarehouseReference);
            OpenCounterpartyReference = new RelayCommand(ShowCounterpartyReference);
            OpenDriverReference = new RelayCommand(ShowDriverReference);

            GeneratePdf = new RelayCommand(ExportToPdf);
            GenerateExcel = new RelayCommand(ExportToExcel);
            OpenSettings = new RelayCommand(ShowSettings);
            OpenHistory = new RelayCommand(ShowHistory);
            OpenHelp = new RelayCommand(ShowHelp);

            WeighingTree_SelectedItemChanged = new RelayCommand(FindWeighting);

            WindowClosingCommand = new RelayCommand(OnWindowClosing);

            ShowPreviewCommand = new RelayCommand(OpenCameraPreview);
            ClosingCameraCommand = new RelayCommand(CloseCameraVideoStream);
            ActivateCameraCommand = new RelayCommand(OpenCameraVideoStream);
            #endregion

            ResetWeighData();
            //SetWeightZero();
            //запуск симуляции весов
            //StartWeightUpdate();

            // Запуск опроса весов
            if (scaleService.isConnect)
                StartPollingAsync();

            InitializeCamerasAsync();
        }

        public async void InitializeCamerasAsync()
        {
            if (Cameras.Count > 0)
            {
                var initializationTasks = Cameras.Select(async camera =>
                {
                    try
                    {
                        if (camera.IsVideo)
                        {
                            camera.IsVideoInRootMenu = true;
                            await camera.InitializationAsync();
                        }
                        else
                        {
                            camera.IsVideoInRootMenu = false;
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show($"У камеры {camera.Name}: {ex.Message}", "Ошибка");
                        camera.IsVideoInRootMenu = false;
                        InitializeCamerasAsync();
                    }
                });

                await Task.WhenAll(initializationTasks);
            }
        }

        public async void OpenCamerasAsync(CameraModel targetCamera)
        {
            var initializationTasks = Cameras.Select(async camera =>
            {
                if (camera.Id == targetCamera.Id)
                {
                    try
                    {
                        camera.IsVideoInRootMenu = true;
                        await camera.InitializationAsync();
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show($"У камеры {camera.Name}: {ex.Message}", "Ошибка");
                        camera.IsVideoInRootMenu = false;
                    }
                }
            });

            await Task.WhenAll(initializationTasks);
        }

        private void LoadHandbooks()
        {
            //TODO только Warehouses находит..
            databaseFirstCode.LoadHandbook("Vehicles", ListVehicle);
            databaseFirstCode.LoadHandbook("CargoTypes", ListCargoType);
            databaseFirstCode.LoadHandbook("Warehouses", ListSourceWarehouse);
            databaseFirstCode.LoadHandbook("Warehouses", ListDestinationWarehouse);
            databaseFirstCode.LoadHandbook("Counterparties", ListCounterparty);
            databaseFirstCode.LoadHandbook("Drivers", ListDriver);
        }

        private void TakePhoto_Def(string weighingType)
        {
            if (SelectedWeighing.Id != -1)
            {
                var directoryPath = "";
                var fileName = "";

                // Формируем полный путь директории и имя файла
                // Пример: photos/год/месяц/дата/id_взвешивания/(TareTime или GrossTime)/
                // Имя файл: (id_взвешивания)_(дата)_(время).jpg
                switch (weighingType)
                {
                    case nameof(SelectedWeighing.TareTime):
                        {
                            directoryPath = Path.Combine(
                                AppDomain.CurrentDomain.BaseDirectory, // Директория приложения
                                "photos",
                                $"{SelectedWeighing.TareTime:yyyy}",
                                $"{SelectedWeighing.TareTime:MMMM}",
                                $"{SelectedWeighing.TareTime:dd.MM.yyyy}",
                                SelectedWeighing.Id.ToString(),
                                nameof(SelectedWeighing.TareTime));

                            fileName = $"{SelectedWeighing.Id}_{SelectedWeighing.TareTime:yyyyMMdd_HHmmss}";
                        }
                        break;
                    case nameof(SelectedWeighing.GrossTime):
                        {
                            directoryPath = Path.Combine(
                                AppDomain.CurrentDomain.BaseDirectory, // Директория приложения
                                "photos",
                                $"{SelectedWeighing.GrossTime:yyyy}",
                                $"{SelectedWeighing.GrossTime:MMMM}",
                                $"{SelectedWeighing.GrossTime:dd.MM.yyyy}",
                                SelectedWeighing.Id.ToString(),
                                nameof(SelectedWeighing.GrossTime));

                            fileName = $"{SelectedWeighing.Id}_{SelectedWeighing.GrossTime:yyyyMMdd_HHmmss}";
                        }
                        break;
                }
                
                try
                {
                    // Выполняем фотофиксацию со всех камер
                    List<byte[]> photos = CapturePhotosFromAllCameras();

                    var i = 0;
                    foreach (var photo in photos)
                    {
                        var fullPath = fileSystemService.SavePhoto(photo, directoryPath, $"{fileName}_{i}.jpg");
                        databaseFirstCode.SavePhotoToSQL(fullPath, SelectedWeighing.Id);
                        SelectedWeighing.PathPhotos.Add(fullPath);
                        i++;
                    }

                    SelectedWeighing.PhotoIndex = SelectedWeighing.PathPhotos.Count - 1;
                    if (SelectedWeighing.PhotoIndex >= 0)
                    {
                        WeighingPhotoSource = SelectedWeighing.PathPhotos[SelectedWeighing.PhotoIndex];
                    }

                    MessageBox.Show("Фотофиксация выполнена", "Фото");
                }
                catch (Exception ex)
                {
                    // Обработка ошибок (например, показ сообщения пользователю)
                    Console.WriteLine($"Ошибка при захвате фото: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Сначала начните взвешивание", "Ошибка");
            }
        }

        #region Имитация получения веса
        private DispatcherTimer weightTimer;
        private Random rand = new Random();
        private void StartWeightUpdate()
        {
            weightTimer = new DispatcherTimer();
            weightTimer.Interval = TimeSpan.FromSeconds(1);
            weightTimer.Tick += (s, e) => UpdateWeight();
            weightTimer.Start();
        }
        private void UpdateWeight()
        {
            CurrentWeight = (int)(rand.Next(1000, 5000) / 100.0);
        }
        #endregion

        private async void StartPollingAsync()
        {
            await scaleService.StartPolling( async weight =>
            {
                CurrentWeight = weight;
            });
        }

        public List<byte[]> CapturePhotosFromAllCameras()
        {

            var photos = new List<byte[]>();

            foreach (var camera in Cameras)
            {
                try
                {
                    if (camera.IsPhoto)
                    {
                        var photo = camera.CapturePhoto();
                        if (photo != null && photo.Length > 0)
                        {
                            photos.Add(photo);
                        }
                    }
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show($"У камеры {camera.Name}: {ex.Message}", "Ошибка");
                    camera.IsPhoto = false;
                }
            }

            return photos;
        }
    }
}
