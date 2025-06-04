using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using WeighingSystem.Helpers;

namespace WeighingSystem.Controls
{
    [TemplatePart(Name = "Suggestions", Type = typeof(ListBox))]
    [TemplatePart(Name = "SuggestionPopup", Type = typeof(Popup))]

    public class Suggestion : Control
    {
        static Suggestion()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Suggestion), new FrameworkPropertyMetadata(typeof(Suggestion)));
        }

        public ListBox Suggestions;
        public Popup SuggestionPopup;

        private TextBox _textBox;
        private ObservableCollection<string> _lstSuggestions;
        private SuggestionFilter Filter { get; set; }
        /// <summary>
        /// Фильтр для предложенных значений, применяется после проверки на null, поэтому её писать не надо.
        /// </summary>
        public delegate bool SuggestionFilter(string input, string suggestion);

        public void InvalidateFields(TextBox textBox, ObservableCollection<string> suggestions, SuggestionFilter filter)
        {
            _textBox = textBox;
            _lstSuggestions = suggestions;
            Filter = filter;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (Template != null)
            {
                Suggestions = Template.FindName("Suggestions", this) as ListBox;
                SuggestionPopup = Template.FindName("SuggestionPopup", this) as Popup;

                Suggestions.PreviewMouseDown += (s, e) => { SuggestionsPreviewMouseDown(e); };
                Suggestions.PreviewKeyDown += (s, e) => { SuggestionsPreviewKeyDown(s, e); };
                Suggestions.KeyDown += (s, e) => { SuggestionsKeyDown(e); };
            }
        }

        private void SuggestionsPreviewMouseDown(MouseButtonEventArgs e)
        {
            /// Выбираем элемент из списка с помощью нажатия ЛКМ
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.OriginalSource is TextBlock textBlock)
                {
                    _textBox.Text = textBlock.Text;
                    SuggestionPopup.IsOpen = false;
                    _textBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void SuggestionsPreviewKeyDown(object sender, KeyEventArgs e)
        {
            /// Обработка выхода за пределы списка listBox
            var listBox = sender as ListBox;

            if (e.Key == Key.Down)
            {
                if (listBox.SelectedIndex == listBox.Items.Count - 1)
                {
                    e.Handled = true;
                    //Console.WriteLine("Достигнут конец списка!");
                }
            }
            else if (e.Key == Key.Up)
            {
                if (listBox.SelectedIndex == 0)
                {
                    Suggestions.SelectedIndex = -1;
                    _textBox.Focus();
                    e.Handled = true;
                    //Console.WriteLine("Достигнуто начало списка!");
                }
            }
        }

        private void SuggestionsKeyDown(KeyEventArgs e)
        {            
            if (e.OriginalSource is ListBoxItem)
            {
                /// Выбираем элемент из списка с помощью нажатия Enter или пишем дальше текст
                if (e.Key == Key.Enter)
                {
                    ListBoxItem listBoxItem = e.OriginalSource as ListBoxItem;
                    _textBox.Text = listBoxItem.Content as string;
                    SuggestionPopup.IsOpen = false;
                    _textBox.Focus();
                    _textBox.CaretIndex = _textBox.Text.Length;
                }
                else if (e.Key != Key.Down && e.Key != Key.Up)
                {
                    if (KeyboardHelper.GetCharFromKey(e.Key) is string character)
                    {
                        // Регулярное выражение для проверки ввода букв, цифр и знаков препинания
                        Regex regex = new Regex(@"[a-zA-Z0-9\p{P}а-яА-ЯёЁ]");
                        if (regex.IsMatch(character))
                        {
                            _textBox.Focus();
                            // Далее textBox обрабатывает нажатие клавиши
                        }
                    }
                }
            }
        }

        public void ShowSuggestion()
        {
            Suggestions.Items.Clear();
            if (_lstSuggestions != null)
                foreach (var suggestion in _lstSuggestions)
                    Suggestions.Items.Add(suggestion);

            SuggestionPopup.IsOpen = Suggestions.Items.Count > 0 && !string.IsNullOrEmpty(_textBox.Text);

            Suggestions.Items.Filter = item =>
            {
                string itemText = item as string;

                if (string.IsNullOrWhiteSpace(_textBox.Text) || string.IsNullOrWhiteSpace(itemText))
                    return false;

                return Filter(_textBox.Text, itemText) && !string.Equals(_textBox.Text,itemText, StringComparison.InvariantCultureIgnoreCase);
            };
        }
    }
}
