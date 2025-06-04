using System;
using System.Windows;
using System.Collections.ObjectModel;
using WeighingSystem.Models;
using System.Linq;
using System.Collections.Generic;

namespace WeighingSystem.Services
{
    public class DatabaseFirstCode
    {
        public DatabaseFirstCode()
        {
            if (!DatabaseHelper.IsCreateFile())
            {
                DatabaseHelper.InitializeDatabase();
                CreateDatabaseIfNotExists();

                FillDemoData();
            }
        }

        private void CreateDatabaseIfNotExists()
        {
            DatabaseHelper.SQLiteCommand(@"
                    CREATE TABLE IF NOT EXISTS Vehicles (id INTEGER PRIMARY KEY AUTOINCREMENT, license_plate TEXT UNIQUE, brand TEXT);
                    CREATE TABLE IF NOT EXISTS CargoTypes (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE);
                    CREATE TABLE IF NOT EXISTS Warehouses (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE, type TEXT);
                    CREATE TABLE IF NOT EXISTS Counterparties (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT UNIQUE, tax_id TEXT UNIQUE);
                    CREATE TABLE IF NOT EXISTS Drivers (id INTEGER PRIMARY KEY AUTOINCREMENT, full_name TEXT UNIQUE);
                    CREATE TABLE IF NOT EXISTS Users (id INTEGER PRIMARY KEY AUTOINCREMENT, username TEXT UNIQUE, password_hash TEXT, role TEXT);
                    
                    CREATE TABLE IF NOT EXISTS Weighings (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        vehicle_id INTEGER, cargo_type_id INTEGER, source_warehouse_id INTEGER, destination_warehouse_id INTEGER,
                        counterparty_id INTEGER, driver_id INTEGER, gross_weight INTEGER, tare_weight INTEGER, net_weight INTEGER,
                        weighing_time TEXT, tare_time TEXT, gross_time TEXT, operator_id INTEGER,
                        FOREIGN KEY(vehicle_id) REFERENCES Vehicles(id),
                        FOREIGN KEY(cargo_type_id) REFERENCES CargoTypes(id),
                        FOREIGN KEY(source_warehouse_id) REFERENCES Warehouses(id),
                        FOREIGN KEY(destination_warehouse_id) REFERENCES Warehouses(id),
                        FOREIGN KEY(counterparty_id) REFERENCES Counterparties(id),
                        FOREIGN KEY(driver_id) REFERENCES Drivers(id),
                        FOREIGN KEY(operator_id) REFERENCES Users(id)
                    );

                    CREATE TABLE IF NOT EXISTS WeighingPhotos (id INTEGER PRIMARY KEY AUTOINCREMENT, weighing_id INTEGER, photo_path TEXT, FOREIGN KEY(weighing_id) REFERENCES Weighings(id));
                    CREATE TABLE IF NOT EXISTS ScaleSettings (id INTEGER PRIMARY KEY AUTOINCREMENT, ip_address TEXT NOT NULL, port INTEGER NOT NULL, poll_interval INTEGER NOT NULL);
                    CREATE TABLE IF NOT EXISTS CamerasSettings (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, address TEXT NOT NULL, is_video BIT NOT NULL DEFAULT 0, is_photo BIT NOT NULL DEFAULT 0);
            ");

            DatabaseHelper.SQLiteCommand(@"
                INSERT OR IGNORE INTO ScaleSettings (id, ip_address, port, poll_interval) VALUES (1, '127.0.0.1', 11600, 200);
            ");
        }

        public bool FillDemoData()
        {
            var result = DatabaseHelper.SQLiteCommand(@"
                INSERT OR IGNORE INTO Vehicles (license_plate, brand) VALUES ('А123УВ77', 'КамАЗ');
                INSERT OR IGNORE INTO CargoTypes (name) VALUES ('Песок');
                INSERT OR IGNORE INTO Warehouses (name, type) VALUES ('Склад 1', 'source');
                INSERT OR IGNORE INTO Warehouses (name, type) VALUES ('Склад 2', 'destination');
                INSERT OR IGNORE INTO Counterparties (name, tax_id) VALUES ('ООО Рога и Копыта', '1234567890');
                INSERT OR IGNORE INTO Drivers (full_name) VALUES ('Иванов Иван Иванович');
                INSERT OR IGNORE INTO Users (username, password_hash, role) VALUES ('admin', 'hash', 'admin');
                INSERT OR IGNORE INTO CamerasSettings (name, address, is_photo) VALUES ('Склад', 'rtsp://admin:12345@192.168.1.248:554/1/1', 1); 
                INSERT OR IGNORE INTO CamerasSettings (name, address) VALUES ('Склад 1', 'rtsp://admin:Tensib505@192.168.1.108 :554/1/1'); 
                INSERT OR IGNORE INTO CamerasSettings (name, address) VALUES ('Склад 2', 'rtsp://admin:admin123@192.168.1.109:554/1/1'); 
            ") > -1;

            Weighing demo = new Weighing
            {
                Id = -1,
                Vehicle = "А123УВ77",
                CargoType = "Песок",
                SourceWarehouse = "Склад 1",
                DestinationWarehouse = "Склад 2",
                Counterparty = "ООО Рога и Копыта",
                Driver = "Иванов Иван Иванович",
                GrossWeight = 250,
                TareWeight = 120,
            };

            SaveCurrentWeighing(demo, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return result;
        }

        public void SaveCamera(CameraModel camera)
        {
            if (camera.Id == -1)
            {
                camera.Id = DatabaseHelper.SQLiteCommand(@"
                        INSERT INTO CamerasSettings (name, address, is_video, is_photo) 
                        VALUES (@name, @address, @is_video, @is_photo);
                    ",
                    new Dictionary<string, object> {
                        { "@name", camera.Name },
                        { "@address", camera.ConnectionString },
                        { "@is_video", camera.IsVideo ? 1 : 0 },
                        { "@is_photo", camera.IsPhoto ? 1 : 0 },
                    },
                    lastInsertRowId: true
                );
            }
            else
            {
                DatabaseHelper.SQLiteCommand(@"
                        UPDATE CamerasSettings 
                        SET name = @name, address = @address, is_video = @is_video, is_photo = @is_photo 
                        WHERE id = @id;
                    ",
                    new Dictionary<string, object> {
                        { "@id", camera.Id },
                        { "@name", camera.Name },
                        { "@address", camera.ConnectionString },
                        { "@is_video", camera.IsVideo ? 1 : 0 },
                        { "@is_photo", camera.IsPhoto ? 1 : 0 },
                    }
                );
            }
        }

        public void RemoveCamera(CameraModel camera)
        {
            DatabaseHelper.SQLiteCommand(
                @"DELETE FROM CamerasSettings WHERE id = @id",
                new Dictionary<string, object> { { "@id", camera.Id } }
            );
        }

        public void LoadCameras(ObservableCollection<CameraModel> cameras)
        {
            DatabaseHelper.SQLiteCommand(@"
                    SELECT 
                    id, name, address, is_video, is_photo
                    FROM CamerasSettings
                ", reader =>
            {
                var id = reader.GetInt32(0);

                var camera = cameras.FirstOrDefault(cameraModel => cameraModel.Id == id);
                if (camera == null)
                {
                    cameras.Add(new CameraModel(
                        id,
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetInt32(3) == 1,
                        reader.GetInt32(4) == 1)
                    );
                }
                else
                {
                    camera.Name = reader.GetString(1);
                    camera.ConnectionString = reader.GetString(2);
                    camera.IsVideo = reader.GetInt32(3) == 1;
                    camera.IsPhoto = reader.GetInt32(4) == 1;
                }
            });
        }

        public void SaveCurrentWeighing(Weighing weighing, string tareTime = null, string grossTime = null)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                DatabaseHelper.ExecuteNonQuery(connection, @"INSERT OR IGNORE INTO Vehicles (license_plate) VALUES (@value)",
                    new Dictionary<string, object> { { "@value", weighing.Vehicle } });

                DatabaseHelper.ExecuteNonQuery(connection, @"INSERT OR IGNORE INTO CargoTypes (name) VALUES (@value)",
                    new Dictionary<string, object> { { "@value", weighing.CargoType } });

                DatabaseHelper.ExecuteNonQuery(connection, @"INSERT OR IGNORE INTO Warehouses (name, type) VALUES (@value, 'source')",
                    new Dictionary<string, object> { { "@value", weighing.SourceWarehouse } });

                DatabaseHelper.ExecuteNonQuery(connection, @"INSERT OR IGNORE INTO Warehouses (name, type) VALUES (@value, 'destination')",
                    new Dictionary<string, object> { { "@value", weighing.DestinationWarehouse } });

                DatabaseHelper.ExecuteNonQuery(connection, @"INSERT OR IGNORE INTO Counterparties (name) VALUES (@value)",
                    new Dictionary<string, object> { { "@value", weighing.Counterparty } });

                DatabaseHelper.ExecuteNonQuery(connection, @"INSERT OR IGNORE INTO Drivers (full_name) VALUES (@value)",
                     new Dictionary<string, object> { { "@value", weighing.Driver } });

                if (weighing.Id == -1)
                {
                    weighing.Id = DatabaseHelper.ExecuteNonQuery(connection, @"
                        INSERT INTO Weighings (vehicle_id, cargo_type_id, source_warehouse_id, destination_warehouse_id, counterparty_id, driver_id, 
                            gross_weight, tare_weight, net_weight, weighing_time, tare_time, gross_time, operator_id)
                        VALUES (
                            (SELECT id FROM Vehicles WHERE license_plate = @vehicle),
                            (SELECT id FROM CargoTypes WHERE name = @cargo),
                            (SELECT id FROM Warehouses WHERE name = @source AND type = 'source'),
                            (SELECT id FROM Warehouses WHERE name = @destination AND type = 'destination'),
                            (SELECT id FROM Counterparties WHERE name = @counterparty),
                            (SELECT id FROM Drivers WHERE full_name = @driver),
                            @gross, @tare, @net, @time, @tare_time, @gross_time, 1)",
                    new Dictionary<string, object> {
                        {"@vehicle", weighing.Vehicle},
                        {"@cargo", weighing.CargoType},
                        {"@source", weighing.SourceWarehouse},
                        {"@destination", weighing.DestinationWarehouse},
                        {"@counterparty", weighing.Counterparty},
                        {"@driver", weighing.Driver},
                        {"@gross", weighing.GrossWeight > 0 ? weighing.GrossWeight : (object)DBNull.Value},
                        {"@tare", weighing.TareWeight > 0 ? weighing.TareWeight : (object)DBNull.Value},
                        {"@net", weighing.NetWeight > 0 ? weighing.NetWeight : (object)DBNull.Value},
                        {"@time", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}"},
                        {"@tare_time", tareTime ?? (object)DBNull.Value},
                        {"@gross_time", grossTime ?? (object)DBNull.Value},
                    },
                    lastInsertRowId: true);
                }
                else
                {
                    DatabaseHelper.ExecuteNonQuery(connection, @"
                        UPDATE Weighings SET
                            vehicle_id = (SELECT id FROM Vehicles WHERE license_plate = @vehicle),
                            cargo_type_id = (SELECT id FROM CargoTypes WHERE name = @cargo),
                            source_warehouse_id = (SELECT id FROM Warehouses WHERE name = @source AND type = 'source'),
                            destination_warehouse_id = (SELECT id FROM Warehouses WHERE name = @destination AND type = 'destination'),
                            counterparty_id = (SELECT id FROM Counterparties WHERE name = @counterparty),
                            driver_id = (SELECT id FROM Drivers WHERE full_name = @driver),
                            gross_weight = COALESCE(@gross, gross_weight),
                            tare_weight = COALESCE(@tare, tare_weight),
                            net_weight =  COALESCE(@net, net_weight),
                            weighing_time = @time,
                            tare_time = COALESCE(@tare_time, tare_time),
                            gross_time = COALESCE(@gross_time, gross_time)
                        WHERE id = @id",
                    new Dictionary<string, object> {
                        {"@vehicle", weighing.Vehicle},
                        {"@cargo", weighing.CargoType},
                        {"@source", weighing.SourceWarehouse},
                        {"@destination", weighing.DestinationWarehouse},
                        {"@counterparty", weighing.Counterparty},
                        {"@driver", weighing.Driver},
                        {"@gross", weighing.GrossWeight > 0 ? weighing.GrossWeight : (object)DBNull.Value},
                        {"@tare", weighing.TareWeight > 0 ? weighing.TareWeight : (object)DBNull.Value},
                        {"@net", weighing.NetWeight > 0 ? weighing.NetWeight : (object)DBNull.Value},
                        {"@time", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}"},
                        {"@tare_time", tareTime ?? (object)DBNull.Value},
                        {"@gross_time", grossTime ?? (object)DBNull.Value},
                        {"@id", weighing.Id},
                    });
                }
            }
        }

        public void SavePhotoToSQL(string photoPath, int currentWeighingId)
        {
            DatabaseHelper.SQLiteCommand(@"
                    INSERT INTO WeighingPhotos
                    (weighing_id, photo_path) VALUES (@weighing_id, @photo_path);
                ",
                new Dictionary<string, object> {
                    { "@weighing_id", currentWeighingId },
                    { "@photo_path", photoPath },
                }
            );
        }

        // выгрузка взвешивания по id
        public Weighing FindWeightingById(int weighingId)
        {
            Weighing weighing = null;

            var isFound = DatabaseHelper.SQLiteCommand(@"
                    SELECT v.license_plate, ct.name, ws.name, wd.name, c.name, d.full_name, 
                        w.tare_weight, w.gross_weight, w.net_weight, w.weighing_time
                    FROM Weighings w
                    LEFT JOIN Vehicles v ON w.vehicle_id = v.id
                    LEFT JOIN CargoTypes ct ON w.cargo_type_id = ct.id
                    LEFT JOIN Warehouses ws ON w.source_warehouse_id = ws.id
                    LEFT JOIN Warehouses wd ON w.destination_warehouse_id = wd.id
                    LEFT JOIN Counterparties c ON w.counterparty_id = c.id
                    LEFT JOIN Drivers d ON w.driver_id = d.id
                    WHERE w.id = @id
                ",
                new Dictionary<string, object> { { "@id", weighingId } },
                reader =>
                {
                    weighing = new Weighing
                    {
                        Vehicle = reader.IsDBNull(0) ? "" : reader.GetString(0),
                        CargoType = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        SourceWarehouse = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        DestinationWarehouse = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Counterparty = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        Driver = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        TareWeight = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                        GrossWeight = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                        //NetWeight  = reader.IsDBNull(8) ? "" : reader.GetDouble(8).ToString("F2"), Само рассчитывается
                        WeighingTime = DateTime.Parse(reader.GetString(9)),
                        Id = weighingId
                    };
                });

            if (weighing == null)
            {
                if (!isFound)
                {
                    MessageBox.Show("Взвешивание не найдено", "Ошибка");
                    return null;
                }
                else
                {
                    MessageBox.Show("Не удалось загрузить данные взвешивания", "Ошибка");
                    return null;
                }
            }
            else
            {
                DatabaseHelper.SQLiteCommand(@"
                    SELECT photo_path FROM WeighingPhotos 
                    WHERE weighing_id = @id ORDER BY id
                ",
                new Dictionary<string, object> { { "@id", weighingId } },
                reader =>
                {
                    weighing.PathPhotos.Add(reader.GetString(0));
                });

                if (weighing.PathPhotos.Count > 0)
                {
                    weighing.PhotoIndex = 0;
                }
                else
                {
                    weighing.PhotoIndex = -1;
                }

                return weighing;
            }
        }

        // выгрузка древа
        public void LoadWeighingTree(ObservableCollection<Location> locations)
        {
            var location = locations.FirstOrDefault(_location => _location.Name == "Автовесовая 1");
            if (location == null)
            {
                location = new Location("Автовесовая 1");
                locations.Add(location);
            }

            int? totalRowCount = null;
            DatabaseHelper.SQLiteCommand(@"
                SELECT w.weighing_time, w.id, v.license_plate, c.name, w.tare_time, w.gross_time, COUNT(*) OVER () AS TotalRowCount
                FROM Weighings w
                JOIN Vehicles v ON w.vehicle_id = v.id
                JOIN Counterparties c ON w.counterparty_id = c.id
                ORDER BY w.weighing_time ASC
            ", reader =>
            {
                //обработка полученных данных
                var weighingTime = DateTime.Parse(reader.GetString(0));
                var yearValue = weighingTime.Year;
                var monthName = $"{weighingTime:MMMM}";
                var dateValue = weighingTime.Day;
                var id = reader.GetInt32(1);
                totalRowCount = totalRowCount == null ? reader.GetInt32(6) : totalRowCount;

                var yearNode = location.Years.FirstOrDefault(year => year.YearValue == yearValue);
                if (yearNode == null)
                {
                    yearNode = new Location.Year(yearValue);
                    location.Years.Add(yearNode);
                }

                var monthNode = yearNode.Months.FirstOrDefault(month => month.MonthName == monthName);
                if (monthNode == null)
                {
                    monthNode = new Location.Month(monthName);
                    yearNode.Months.Add(monthNode);
                }

                var dateNode = monthNode.Dates.FirstOrDefault(date => date.DateValue == dateValue);
                if (dateNode == null)
                {
                    dateNode = new Location.Date(dateValue);
                    monthNode.Dates.Add(dateNode);
                }

                var weighing = dateNode.Weighings.FirstOrDefault(weigh => weigh.Id == id);
                if (weighing == null)
                {
                    //пока это все что нужно для отображения в древе взвешиваний
                    weighing = new Weighing
                    {
                        Id = id,
                        Vehicle = reader.GetString(2),
                        Counterparty = reader.GetString(3),
                        WeighingTime = weighingTime,
                        TareTime = reader.IsDBNull(4) ? null : (DateTime?)DateTime.Parse(reader.GetString(4)),
                        GrossTime = reader.IsDBNull(5) ? null : (DateTime?)DateTime.Parse(reader.GetString(5)),

                    };
                    dateNode.Weighings.Insert(0, weighing);
                }
                else
                {
                    weighing.Vehicle = reader.GetString(2);
                    weighing.Counterparty = reader.GetString(3);
                    weighing.WeighingTime = weighingTime;
                    weighing.TareTime = reader.IsDBNull(4) ? null : (DateTime?)DateTime.Parse(reader.GetString(4));
                    weighing.GrossTime = reader.IsDBNull(5) ? null : (DateTime?)DateTime.Parse(reader.GetString(5));
                }

                if (--totalRowCount == 1)
                {
                    location.IsExpanded = true;
                    yearNode.IsExpanded = true;
                    monthNode.IsExpanded = true;
                    dateNode.IsExpanded = true;
                }
            });
        }

        public void LoadHandbook(string listName, ObservableCollection<string> list)
        {
            list.Clear();

            var query = "";
            switch (listName)
            {
                case "Vehicles":
                    {
                        query = "SELECT license_plate FROM Vehicles";
                    }
                    break;
                case "Drivers":
                    {
                        query = "SELECT full_name FROM Drivers";
                    }
                    break;
                default:
                    {
                        query = $"SELECT name FROM {listName}";
                    }
                    break;
            }

            DatabaseHelper.SQLiteCommand(query,reader =>
            {
                list.Add(reader.IsDBNull(0) ? "—" : reader.GetString(0));
            });
        }
    }
}