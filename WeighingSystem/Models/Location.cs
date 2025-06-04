using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace WeighingSystem.Models
{
    // Модель для локации
    public class Location : TreeItemBase
    {
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
        public ObservableCollection<Year> Years { get ; set; }

        public Location(string name, ObservableCollection<Year> years = null)
        {
            Name = name;
            Years = years ?? new ObservableCollection<Year>();
        }

        public Weighing FindWeightingByDateAndId(DateTime dateTime, int id)
        {
            return Years.FirstOrDefault(year => year.YearValue == dateTime.Year)?.FindWeightingByDateAndId(dateTime, id);
        }

        // Модель для года
        public class Year: TreeItemBase
        {
            public int YearValue { get; set; }
            public ObservableCollection<Month> Months { get; set; }

            /// <summary>
            /// yearValue == DateTime.Year
            /// </summary>
            public Year(int yearValue, ObservableCollection<Month> months = null)
            {
                YearValue = yearValue;
                Months = months ?? new ObservableCollection<Month>();
            }

            public Weighing FindWeightingByDateAndId(DateTime dateTime, int id)
            {
                return Months.FirstOrDefault(month => month.MonthName == dateTime.ToString("MMMM"))?.FindWeightingByDateAndId(dateTime, id);
            }
        }

        // Модель для месяца
        public class Month: TreeItemBase
        {
            public string MonthName { get; set; }
            public ObservableCollection<Date> Dates { get; set; }

            /// <summary>
            /// monthName == "MMMM"
            /// </summary>
            public Month(string monthName, ObservableCollection<Date> dates = null)
            {
                MonthName = monthName;
                Dates = dates ?? new ObservableCollection<Date>();
            }

            public Weighing FindWeightingByDateAndId(DateTime dateTime, int id)
            {
                return Dates.FirstOrDefault(date => date.DateValue == dateTime.Day)?.FindWeightingById(id);
            }
        }

        //Модель для даты
        public class Date: TreeItemBase
        {
            public int DateValue { get; set; }
            public ObservableCollection<Weighing> Weighings { get; set; }

            /// <summary>
            /// dateValue == dateTime.Day
            /// </summary>
            public Date(int dateValue, ObservableCollection<Weighing> weighings = null)
            {
                DateValue = dateValue;
                Weighings = weighings ?? new ObservableCollection<Weighing>();
            }

            public Weighing FindWeightingById(int id)
            {
                return Weighings.FirstOrDefault(weighing => weighing.Id == id);
            }
        }
    }

    public abstract class TreeItemBase: BasicModel
    {
        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
    }
}
