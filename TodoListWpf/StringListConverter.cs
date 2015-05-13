using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace TodoListWpf
{
    public class StringListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringList = value as IEnumerable<string>;
            if (stringList != null)
            {
                return string.Join(", ", stringList);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}