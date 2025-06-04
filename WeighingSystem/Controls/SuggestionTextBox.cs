using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WeighingSystem.Controls
{
    [TemplatePart(Name = "SuggestionEditor", Type = typeof(TextBox))]

    public class SuggestionTextBox : Control
    {
        static SuggestionTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SuggestionTextBox), new FrameworkPropertyMetadata(typeof(SuggestionTextBox)));
        }

        private TextBox textBox;
        private Suggestion suggestion;

        private readonly Dictionary<FilterType, Suggestion.SuggestionFilter> _filters = new Dictionary<FilterType, Suggestion.SuggestionFilter>();

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (Template != null)
            {
                textBox = Template.FindName("SuggestionEditor", this) as TextBox;
                suggestion = Template.FindName("Suggestion", this) as Suggestion;

                InvalidateFilters();

                suggestion.InvalidateFields(textBox, Suggestions, _filters[Filter]);

                textBox.PreviewKeyDown += (s, e) => { TextBoxPreviewKeyDown(e); };
                textBox.KeyDown += (s, e) => { TextBoxKeyDown(e); };
                textBox.TextChanged += (s, e) => { TextBoxTextChanged(); };
            }
        }

        private void TextBoxPreviewKeyDown(KeyEventArgs e)
        {
            /// Переход в список
            if (e.Key == Key.Down && suggestion.Suggestions.Items.Count > 0 && !(e.OriginalSource is ListBoxItem))
            {
                suggestion.Suggestions.Focus();
                suggestion.Suggestions.SelectedIndex = 0;
                ListBoxItem listBoxItem = suggestion.Suggestions.ItemContainerGenerator.ContainerFromItem(suggestion.Suggestions.Items[suggestion.Suggestions.SelectedIndex])
                    as ListBoxItem;
                listBoxItem.Focus();
                e.Handled = true;
            }
        }

        private void TextBoxKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                suggestion.SuggestionPopup.IsOpen = false;
            }
        }

        private void TextBoxTextChanged()
        {
            suggestion.ShowSuggestion();
            TextValue = textBox.Text;
        }

        #region Поля XAML
        /// <summary>
        /// Тип фильтра, который будет применяться к предложениям.
        /// </summary>
        /// <remarks>
        /// Фильтр используется для проверки соответствия предложений введенным данным.
        /// Значение по умолчанию: <see cref="FilterType.MatchAllWords"/>.
        /// </remarks>
        public FilterType Filter
        {
            get => (FilterType)GetValue(FilterProperty);
            set => SetValue(FilterProperty, value);
        }
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register(nameof(Filter), typeof(FilterType), typeof(SuggestionTextBox),
                new PropertyMetadata(FilterType.StartsAllWith));

        public string TextValue
        {
            get { return (string)GetValue(TextValueProperty); }
            set { SetValue(TextValueProperty, value); }
        }
        public static readonly DependencyProperty TextValueProperty =
            DependencyProperty.Register(nameof(TextValue), typeof(string), typeof(SuggestionTextBox),
                new PropertyMetadata(default(string), OnTextValueChanged));

        private static void OnTextValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SuggestionTextBox suggestionTextBox && e.NewValue is string newText)
            {
                if (suggestionTextBox.textBox != null)
                {
                    suggestionTextBox.textBox.Text = newText;
                }
            }
        }

        public ObservableCollection<string> Suggestions
        {
            get { return (ObservableCollection<string>)GetValue(SuggestionsProperty); }
            set { SetValue(SuggestionsProperty, value); }
        }
        public static readonly DependencyProperty SuggestionsProperty =
            DependencyProperty.Register(nameof(Suggestions), typeof(ObservableCollection<string>), typeof(SuggestionTextBox),
                new PropertyMetadata(default));
        #endregion

        #region SuggestionFilter
        private void InvalidateFilters()
        {
            _filters[FilterType.StartsAllWith] = StartsAllWith;
            _filters[FilterType.StartsWith] = StartsWith;
        }

        public enum FilterType
        {
            /// <summary>
            /// Проверяет, что каждое слово из введенной строки соответствует хотя бы одному слову из предложения.
            /// </summary>
            StartsAllWith,

            /// <summary>
            /// Проверяет, что предложение начинается с введенного текста.
            /// </summary>
            StartsWith
        }

        private Suggestion.SuggestionFilter StartsAllWith => (string input, string suggestion) =>
        {
            // Разделяем введенную строку на слова
            var inputWords = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Разделяем предлагаемую строку на слова
            var suggestionWords = suggestion.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Проверяем каждое слово из введенной строки
            foreach (var inputWord in inputWords)
            {
                // Проверяем, есть ли хотя бы одно слово в предлагаемой строке,
                // которое начинается с текущего слова из введенной строки

                // Если для текущего слова нет совпадения, возвращаем false
                if (!suggestionWords.Any(suggestionWord => suggestionWord.StartsWith(inputWord, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            // Если все слова из введенной строки нашли соответствие, возвращаем true
            return true;
        };

        private Suggestion.SuggestionFilter StartsWith => (string input, string suggestion) =>
        {
            // Проверяем, начинается ли предложение с введенного текста (без учета регистра)
            return suggestion.StartsWith(input, StringComparison.OrdinalIgnoreCase);
        };
        #endregion
    }
}
