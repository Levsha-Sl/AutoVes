using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace WeighingSystem.Controls
{
    public class RegexValidationRule : ValidationRule
    {
        public string Pattern { get; set; }
        public string Example { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string text)
            {
                // Проверяем текст на соответствие регулярному выражению
                Regex regex = new Regex(Pattern);
                if (regex.IsMatch(text))
                {
                    return ValidationResult.ValidResult;
                }
            }

            // Если текст не соответствует, возвращаем ошибку
            return new ValidationResult(false, $"Текст не соответствует шаблону:\r{Example}");
        }
    }
}
