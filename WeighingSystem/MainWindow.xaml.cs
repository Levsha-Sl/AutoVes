using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Linq;
using System.Reflection;

namespace WeighingSystem
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer weightTimer;
        private Random rand = new Random();
        private int photoIndex = 0;
        private List<string> currentPhotos = new List<string>();
        private double weight;
        private string connectionString = "Data Source=weighing_system.db;Version=3;";
        private int currentWeighingId = -1;
        private const string ScaleName = "Автовесовая 1";

        public MainWindow()
        {
            InitializeComponent();
            CreateDatabaseIfNotExists();
            LoadInitialData();
            StartWeightUpdate();
            WeighingTree.SelectedItemChanged += WeighingTree_SelectedItemChanged;
        }

        private void CreateDatabaseIfNotExists()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Vehicles (id INTEGER PRIMARY KEY AUTOINCREMENT, license_plate TEXT UNIQUE, brand TEXT);
                    CREATE TABLE IF NOT EXISTS CargoTypes (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE);
                    CREATE TABLE IF NOT EXISTS Warehouses (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE, type TEXT);
                    CREATE TABLE IF NOT EXISTS Counterparties (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, tax_id TEXT UNIQUE);
                    CREATE TABLE IF NOT EXISTS Drivers (id INTEGER PRIMARY KEY AUTOINCREMENT, full_name TEXT);
                    CREATE TABLE IF NOT EXISTS Users (id INTEGER PRIMARY KEY AUTOINCREMENT, username TEXT UNIQUE, password_hash TEXT, role TEXT);
                    CREATE TABLE IF NOT EXISTS Weighings (id INTEGER PRIMARY KEY AUTOINCREMENT, vehicle_id INTEGER, cargo_type_id INTEGER, 
                        source_warehouse_id INTEGER, destination_warehouse_id INTEGER, counterparty_id INTEGER, driver_id INTEGER, 
                        gross_weight REAL, tare_weight REAL, net_weight REAL, weighing_time TEXT, tare_time TEXT, brutto_time TEXT, operator_id INTEGER, 
                        FOREIGN KEY(vehicle_id) REFERENCES Vehicles(id), FOREIGN KEY(cargo_type_id) REFERENCES CargoTypes(id), 
                        FOREIGN KEY(source_warehouse_id) REFERENCES Warehouses(id), FOREIGN KEY(destination_warehouse_id) REFERENCES Warehouses(id), 
                        FOREIGN KEY(counterparty_id) REFERENCES Counterparties(id), FOREIGN KEY(driver_id) REFERENCES Drivers(id), 
                        FOREIGN KEY(operator_id) REFERENCES Users(id));
                    CREATE TABLE IF NOT EXISTS WeighingPhotos (id INTEGER PRIMARY KEY AUTOINCREMENT, weighing_id INTEGER, photo_path TEXT, 
                        FOREIGN KEY(weighing_id) REFERENCES Weighings(id));";
                command.ExecuteNonQuery();
            }
        }

        private void LoadInitialData()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                // Загрузка справочников
                LoadComboBox(command, "SELECT license_plate FROM Vehicles", VehicleComboBox);
                LoadComboBox(command, "SELECT name FROM CargoTypes", CargoTypeComboBox);
                LoadComboBox(command, "SELECT name FROM Warehouses", SourceWarehouseComboBox);
                LoadComboBox(command, "SELECT name FROM Warehouses", DestinationWarehouseComboBox);
                LoadComboBox(command, "SELECT name FROM Counterparties", CounterpartyComboBox);
                LoadComboBox(command, "SELECT full_name FROM Drivers", DriverComboBox);

                // Загрузка дерева взвешиваний
                LoadWeighingTree(connection);
            }
        }

        private void LoadComboBox(SQLiteCommand command, string query, ComboBox comboBox)
        {
            comboBox.Items.Clear();
            command.CommandText = query;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read()) comboBox.Items.Add(reader.GetString(0));
            }
        }

        private void LoadWeighingTree(SQLiteConnection connection)
        {
            WeighingTree.Items.Clear();
            var scaleItem = new TreeViewItem { Header = ScaleName };
            WeighingTree.Items.Add(scaleItem);

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT w.id, w.weighing_time, w.tare_time, w.brutto_time, v.license_plate, c.name
                FROM Weighings w
                JOIN Vehicles v ON w.vehicle_id = v.id
                JOIN Counterparties c ON w.counterparty_id = c.id
                ORDER BY w.weighing_time DESC";
            using (var reader = command.ExecuteReader())
            {
                var weighingsByYear = new Dictionary<string, TreeViewItem>();
                while (reader.Read())
                {
                    int weighingId = reader.GetInt32(0);
                    DateTime weighingTime = DateTime.Parse(reader.GetString(1));
                    string tareTime = reader.IsDBNull(2) ? "—" : reader.GetString(2);
                    string bruttoTime = reader.IsDBNull(3) ? "—" : reader.GetString(3);
                    string vehicle = reader.GetString(4);
                    string counterparty = reader.GetString(5);

                    string year = weighingTime.Year.ToString();
                    string month = weighingTime.ToString("MMMM");
                    string day = weighingTime.Day.ToString();

                    if (!weighingsByYear.ContainsKey(year))
                    {
                        var yearItem = new TreeViewItem { Header = year };
                        scaleItem.Items.Add(yearItem);
                        weighingsByYear[year] = yearItem;
                    }

                    var yearNode = weighingsByYear[year];
                    var monthNode = yearNode.Items.Cast<TreeViewItem>().FirstOrDefault(i => (string)i.Header == month);
                    if (monthNode == null)
                    {
                        monthNode = new TreeViewItem { Header = month };
                        yearNode.Items.Add(monthNode);
                    }

                    var dayNode = monthNode.Items.Cast<TreeViewItem>().FirstOrDefault(i => (string)i.Header == day);
                    if (dayNode == null)
                    {
                        dayNode = new TreeViewItem { Header = day };
                        monthNode.Items.Add(dayNode);
                    }

                    var weighingItem = new TreeViewItem
                    {
                        Header = $"Тара: {tareTime} | Брутто: {bruttoTime} | Авто: {vehicle} | Контрагент: {counterparty}",
                        Tag = weighingId
                    };
                    dayNode.Items.Add(weighingItem);
                }
            }
            scaleItem.IsExpanded = true;
        }

        private void StartWeightUpdate()
        {
            weightTimer = new DispatcherTimer();
            weightTimer.Interval = TimeSpan.FromSeconds(1);
            weightTimer.Tick += (s, e) => UpdateWeight();
            weightTimer.Start();
        }

        private void UpdateWeight()
        {
            weight = rand.Next(1000, 5000) / 100.0;
            CurrentWeightText.Text = $"{weight:F2} кг";
        }

        private void ResetWeight_Click(object sender, RoutedEventArgs e)
        {
            CurrentWeightText.Text = "0.00 кг";
            weight = 0;
            BruttoTextBox.Text = "";
            TareTextBox.Text = "";
            NettoTextBox.Text = "";
            VehicleComboBox.Text = "";
            CargoTypeComboBox.Text = "";
            SourceWarehouseComboBox.Text = "";
            DestinationWarehouseComboBox.Text = "";
            CounterpartyComboBox.Text = "";
            DriverComboBox.Text = "";
            currentWeighingId = -1;
            photoIndex = 0;
            currentPhotos.Clear();
            WeighingPhoto.Source = new BitmapImage(new Uri("placeholder.jpg"));
        }

        private void SaveTare_Click(object sender, RoutedEventArgs e)
        {
            TareTextBox.Text = $"{weight:F2}";
            if (!string.IsNullOrEmpty(BruttoTextBox.Text))
            {
                NettoTextBox.Text = (Convert.ToDouble(BruttoTextBox.Text) - Convert.ToDouble(TareTextBox.Text)).ToString("F2");
            }
            SaveCurrentWeighing(DateTime.Now.ToString("HH:mm:ss"), null);
            MessageBox.Show("Тара сохранена", "Успех");

            TakePhoto_Def();
        }

        private void SaveBrutto_Click(object sender, RoutedEventArgs e)
        {
            BruttoTextBox.Text = $"{weight:F2}";
            if (!string.IsNullOrEmpty(TareTextBox.Text))
            {
                NettoTextBox.Text = (Convert.ToDouble(BruttoTextBox.Text) - Convert.ToDouble(TareTextBox.Text)).ToString("F2");
            }
            SaveCurrentWeighing(null, DateTime.Now.ToString("HH:mm:ss"));
            MessageBox.Show("Брутто сохранено", "Успех");

            TakePhoto_Def();
        }

        private void SaveCurrentWeighing(string tareTime = null, string bruttoTime = null)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = "INSERT OR IGNORE INTO Vehicles (license_plate) VALUES (@value)";
                command.Parameters.AddWithValue("@value", VehicleComboBox.Text);
                command.ExecuteNonQuery();

                command.CommandText = "INSERT OR IGNORE INTO CargoTypes (name) VALUES (@value)";
                command.Parameters.AddWithValue("@value", CargoTypeComboBox.Text);
                command.ExecuteNonQuery();

                command.CommandText = "INSERT OR IGNORE INTO Warehouses (name, type) VALUES (@value, 'source')";
                command.Parameters.AddWithValue("@value", SourceWarehouseComboBox.Text);
                command.ExecuteNonQuery();

                command.CommandText = "INSERT OR IGNORE INTO Warehouses (name, type) VALUES (@value, 'destination')";
                command.Parameters.AddWithValue("@value", DestinationWarehouseComboBox.Text);
                command.ExecuteNonQuery();

                command.CommandText = "INSERT OR IGNORE INTO Counterparties (name) VALUES (@value)";
                command.Parameters.AddWithValue("@value", CounterpartyComboBox.Text);
                command.ExecuteNonQuery();

                command.CommandText = "INSERT OR IGNORE INTO Drivers (full_name) VALUES (@value)";
                command.Parameters.AddWithValue("@value", DriverComboBox.Text);
                command.ExecuteNonQuery();

                if (currentWeighingId == -1)
                {
                    command.CommandText = @"
                        INSERT INTO Weighings (vehicle_id, cargo_type_id, source_warehouse_id, destination_warehouse_id, counterparty_id, driver_id, 
                            gross_weight, tare_weight, net_weight, weighing_time, tare_time, brutto_time, operator_id)
                        VALUES (
                            (SELECT id FROM Vehicles WHERE license_plate = @vehicle),
                            (SELECT id FROM CargoTypes WHERE name = @cargo),
                            (SELECT id FROM Warehouses WHERE name = @source AND type = 'source'),
                            (SELECT id FROM Warehouses WHERE name = @destination AND type = 'destination'),
                            (SELECT id FROM Counterparties WHERE name = @counterparty),
                            (SELECT id FROM Drivers WHERE full_name = @driver),
                            @gross, @tare, @net, @time, @tare_time, @brutto_time, 1)";
                    command.Parameters.AddWithValue("@vehicle", VehicleComboBox.Text);
                    command.Parameters.AddWithValue("@cargo", CargoTypeComboBox.Text);
                    command.Parameters.AddWithValue("@source", SourceWarehouseComboBox.Text);
                    command.Parameters.AddWithValue("@destination", DestinationWarehouseComboBox.Text);
                    command.Parameters.AddWithValue("@counterparty", CounterpartyComboBox.Text);
                    command.Parameters.AddWithValue("@driver", DriverComboBox.Text);
                    command.Parameters.AddWithValue("@gross", string.IsNullOrEmpty(BruttoTextBox.Text) ? (object)DBNull.Value : Convert.ToDouble(BruttoTextBox.Text));
                    command.Parameters.AddWithValue("@tare", string.IsNullOrEmpty(TareTextBox.Text) ? (object)DBNull.Value : Convert.ToDouble(TareTextBox.Text));
                    command.Parameters.AddWithValue("@net", string.IsNullOrEmpty(NettoTextBox.Text) ? (object)DBNull.Value : Convert.ToDouble(NettoTextBox.Text));
                    command.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("@tare_time", tareTime ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@brutto_time", bruttoTime ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();

                    command.CommandText = "SELECT last_insert_rowid()";
                    currentWeighingId = Convert.ToInt32(command.ExecuteScalar());
                }
                else
                {
                    command.CommandText = @"
                        UPDATE Weighings SET
                            vehicle_id = (SELECT id FROM Vehicles WHERE license_plate = @vehicle),
                            cargo_type_id = (SELECT id FROM CargoTypes WHERE name = @cargo),
                            source_warehouse_id = (SELECT id FROM Warehouses WHERE name = @source AND type = 'source'),
                            destination_warehouse_id = (SELECT id FROM Warehouses WHERE name = @destination AND type = 'destination'),
                            counterparty_id = (SELECT id FROM Counterparties WHERE name = @counterparty),
                            driver_id = (SELECT id FROM Drivers WHERE full_name = @driver),
                            gross_weight = @gross,
                            tare_weight = @tare,
                            net_weight = @net,
                            weighing_time = @time,
                            tare_time = COALESCE(@tare_time, tare_time),
                            brutto_time = COALESCE(@brutto_time, brutto_time)
                        WHERE id = @id";
                    command.Parameters.AddWithValue("@vehicle", VehicleComboBox.Text);
                    command.Parameters.AddWithValue("@cargo", CargoTypeComboBox.Text);
                    command.Parameters.AddWithValue("@source", SourceWarehouseComboBox.Text);
                    command.Parameters.AddWithValue("@destination", DestinationWarehouseComboBox.Text);
                    command.Parameters.AddWithValue("@counterparty", CounterpartyComboBox.Text);
                    command.Parameters.AddWithValue("@driver", DriverComboBox.Text);
                    command.Parameters.AddWithValue("@gross", string.IsNullOrEmpty(BruttoTextBox.Text) ? (object)DBNull.Value : Convert.ToDouble(BruttoTextBox.Text));
                    command.Parameters.AddWithValue("@tare", string.IsNullOrEmpty(TareTextBox.Text) ? (object)DBNull.Value : Convert.ToDouble(TareTextBox.Text));
                    command.Parameters.AddWithValue("@net", string.IsNullOrEmpty(NettoTextBox.Text) ? (object)DBNull.Value : Convert.ToDouble(NettoTextBox.Text));
                    command.Parameters.AddWithValue("@time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("@tare_time", tareTime ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@brutto_time", bruttoTime ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@id", currentWeighingId);
                    command.ExecuteNonQuery();
                }

                LoadWeighingTree(connection);
            }
        }

        private void SaveWeighing_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentWeighing();
            MessageBox.Show("Взвешивание сохранено", "Успех");
            ResetWeight_Click(sender, e);
        }

        private void TakePhoto_Def()
        {
            if (currentWeighingId != -1)
            {
                string photoPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + $"\\Photos\\photo{rand.Next(1, 4)}.jpg";
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "INSERT INTO WeighingPhotos (weighing_id, photo_path) VALUES (@weighing_id, @photo_path)";
                    command.Parameters.AddWithValue("@weighing_id", currentWeighingId);
                    command.Parameters.AddWithValue("@photo_path", photoPath);
                    command.ExecuteNonQuery();
                }
                currentPhotos.Add(photoPath);
                WeighingPhoto.Source = new BitmapImage(new Uri(photoPath));
                photoIndex = currentPhotos.Count - 1;
                MessageBox.Show("Фотофиксация выполнена", "Фото");
            }
            else
            {
                MessageBox.Show("Сначала начните взвешивание", "Ошибка");
            }
        }

        private void PreviousPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (photoIndex > 0)
            {
                photoIndex--;
                WeighingPhoto.Source = new BitmapImage(new Uri(currentPhotos[photoIndex]));
            }
        }

        private void NextPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (photoIndex < currentPhotos.Count - 1)
            {
                photoIndex++;
                WeighingPhoto.Source = new BitmapImage(new Uri(currentPhotos[photoIndex]));
            }
        }

        private void WeighingTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = WeighingTree.SelectedItem as TreeViewItem;
            if (selectedItem != null && selectedItem.Tag != null)
            {
                int weighingId = (int)selectedItem.Tag;
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = @"
                        SELECT v.license_plate, ct.name, ws.name, wd.name, c.name, d.full_name, 
                               w.tare_weight, w.gross_weight, w.net_weight
                        FROM Weighings w
                        LEFT JOIN Vehicles v ON w.vehicle_id = v.id
                        LEFT JOIN CargoTypes ct ON w.cargo_type_id = ct.id
                        LEFT JOIN Warehouses ws ON w.source_warehouse_id = ws.id
                        LEFT JOIN Warehouses wd ON w.destination_warehouse_id = wd.id
                        LEFT JOIN Counterparties c ON w.counterparty_id = c.id
                        LEFT JOIN Drivers d ON w.driver_id = d.id
                        WHERE w.id = @id";
                    command.Parameters.AddWithValue("@id", weighingId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            VehicleComboBox.Text = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            CargoTypeComboBox.Text = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            SourceWarehouseComboBox.Text = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            DestinationWarehouseComboBox.Text = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            CounterpartyComboBox.Text = reader.IsDBNull(4) ? "" : reader.GetString(4);
                            DriverComboBox.Text = reader.IsDBNull(5) ? "" : reader.GetString(5);
                            TareTextBox.Text = reader.IsDBNull(6) ? "" : reader.GetDouble(6).ToString("F2");
                            BruttoTextBox.Text = reader.IsDBNull(7) ? "" : reader.GetDouble(7).ToString("F2");
                            NettoTextBox.Text = reader.IsDBNull(8) ? "" : reader.GetDouble(8).ToString("F2");
                            currentWeighingId = weighingId;
                        }
                        else
                        {
                            MessageBox.Show("Не удалось загрузить данные взвешивания", "Ошибка");
                            return;
                        }
                    }

                    currentPhotos.Clear();
                    command.CommandText = "SELECT photo_path FROM WeighingPhotos WHERE weighing_id = @id ORDER BY id";
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@id", weighingId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            currentPhotos.Add(reader.GetString(0));
                        }
                    }

                    if (currentPhotos.Count > 0)
                    {
                        photoIndex = 0;
                        WeighingPhoto.Source = new BitmapImage(new Uri(currentPhotos[photoIndex]));
                    }
                    else
                    {
                        photoIndex = 0;
                        WeighingPhoto.Source = new BitmapImage(new Uri("placeholder.jpg"));
                    }
                }
            }
        }

        // Обработчики кнопок справочников
        private void OpenVehicleReference_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReferenceWindow("Vehicles", "license_plate", "Справочник автомобилей");
            window.Closed += (s, args) => LoadInitialData();
            window.ShowDialog();
        }

        private void OpenCargoTypeReference_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReferenceWindow("CargoTypes", "name", "Справочник типов грузов");
            window.Closed += (s, args) => LoadInitialData();
            window.ShowDialog();
        }

        private void OpenSourceWarehouseReference_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReferenceWindow("Warehouses", "name", "Справочник складов");
            window.Closed += (s, args) => LoadInitialData();
            window.ShowDialog();
        }

        private void OpenDestinationWarehouseReference_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReferenceWindow("Warehouses", "name", "Справочник складов");
            window.Closed += (s, args) => LoadInitialData();
            window.ShowDialog();
        }

        private void OpenCounterpartyReference_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReferenceWindow("Counterparties", "name", "Справочник контрагентов");
            window.Closed += (s, args) => LoadInitialData();
            window.ShowDialog();
        }

        private void OpenDriverReference_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReferenceWindow("Drivers", "full_name", "Справочник водителей");
            window.Closed += (s, args) => LoadInitialData();
            window.ShowDialog();
        }

        private void GeneratePdf_Click(object sender, RoutedEventArgs e) => MessageBox.Show("PDF отчет сгенерирован (заглушка)", "Отчет");
        private void GenerateExcel_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Excel отчет сгенерирован (заглушка)", "Отчет");
        private void OpenSettings_Click(object sender, RoutedEventArgs e) => new SettingsWindow().ShowDialog();
        private void OpenHistory_Click(object sender, RoutedEventArgs e) => new HistoryWindow().ShowDialog();
        private void OpenHelp_Click(object sender, RoutedEventArgs e) => new HelpWindow().ShowDialog();

        private void Window_Loaded(object sender, RoutedEventArgs e) { }
    }
}