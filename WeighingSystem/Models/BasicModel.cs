using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace WeighingSystem.Models
{
    /// <summary>
    /// Реализован INotifyPropertyChanged
    /// </summary>
    public class BasicModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Здесь [CallerMemberName] автоматически подставляет имя вызывающего свойства (Name) в метод OnPropertyChanged.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Здесь мы передаем лямбда-выражение () => Name в метод OnPropertyChanged. 
        /// </summary>
        protected void OnPropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpression)
            {
                string propertyName = memberExpression.Member.Name;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                throw new ArgumentException("Выражение должно быть ссылкой на свойство.");
            }
        }

        public event EventHandler<string> OnError;

        /// <summary>
        /// Генерируем событие об ошибке
        /// </summary>
        /// <param name="message"></param>
        protected void HandleError(string message)
        {
            OnError?.Invoke(this, message);
        }
    }
}
