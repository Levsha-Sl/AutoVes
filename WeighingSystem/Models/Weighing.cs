using System;
using System.Collections.ObjectModel;

namespace WeighingSystem.Models
{
    public class Weighing : TreeItemBase
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

        private int _tareWeight;
        public int TareWeight
        {
            get { return _tareWeight; }
            set
            {
                _tareWeight = value;
                OnPropertyChanged();
                OnPropertyChanged(() => NetWeight);
            }
        }

        private int _grossWeight;
        public int GrossWeight
        {
            get { return _grossWeight; }
            set
            {
                _grossWeight = value;
                OnPropertyChanged();
                OnPropertyChanged(() => NetWeight);
            }
        }

        public int NetWeight
        {
            get { return GrossWeight > 0 && TareWeight >= 0 ? GrossWeight - TareWeight : 0; }
        }

        private string _vehicle;
        public string Vehicle
        {
            get { return _vehicle; }
            set
            {
                _vehicle = value;
                OnPropertyChanged();
            }
        }

        private string _cargoType;
        public string CargoType
        {
            get { return _cargoType; }
            set
            {
                _cargoType = value;
                OnPropertyChanged();
            }
        }

        private string _sourceWarehouse;
        public string SourceWarehouse
        {
            get { return _sourceWarehouse; }
            set
            {
                _sourceWarehouse = value;
                OnPropertyChanged();
            }
        }

        private string _destinationWarehouse;
        public string DestinationWarehouse
        {
            get { return _destinationWarehouse; }
            set
            {
                _destinationWarehouse = value;
                OnPropertyChanged();
            }
        }

        private string _counterparty;
        public string Counterparty
        {
            get { return _counterparty; }
            set
            {
                _counterparty = value;
                OnPropertyChanged();
            }
        }

        private string _driver;
        public string Driver
        {
            get { return _driver; }
            set
            {
                _driver = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> _pathPhotos = new ObservableCollection<string>();
        public ObservableCollection<string> PathPhotos
        {
            get { return _pathPhotos; }
            set
            {
                _pathPhotos = value;
                OnPropertyChanged();
            }
        }

        private int _photoIndex;
        public int PhotoIndex
        {
            get { return _photoIndex; }
            set
            {
                _photoIndex = value;
                OnPropertyChanged();
            }
        }

        //"HH:mm:ss"
        private DateTime? _tareTime;
        public DateTime? TareTime
        {
            get { return _tareTime; }
            set
            {
                _tareTime = value;
                OnPropertyChanged();
            }
        }

        private DateTime? _grossTime;
        public DateTime? GrossTime
        {
            get { return _grossTime; }
            set
            {
                _grossTime = value;
                OnPropertyChanged();
            }
        }
        private DateTime _weighingTime;
        public DateTime WeighingTime
        {
            get { return _weighingTime; }
            set
            {
                _weighingTime = value;
                OnPropertyChanged();
            }
        }

        public Weighing()
        {

        }


        //TODO написать поддержку сравнения
    }
}
