using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Data;

namespace TodoListUniversalWindows10
{
    public class StringListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var stringList = value as IEnumerable<string>;
            if (stringList != null)
            {
                return string.Join(", ", stringList);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}