using System;
using System.Globalization;
using System.Windows.Data;

namespace WorkoutCoachV2.App.View
{
    
    public class SessionDateConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return null;

            static DateTime? PickDate(object obj)
            {
                var t = obj.GetType();
                object? v =
                    t.GetProperty("ScheduledOn")?.GetValue(obj) ??
                    t.GetProperty("Date")?.GetValue(obj) ??
                    t.GetProperty("ScheduledAt")?.GetValue(obj) ??
                    t.GetProperty("PerformedOn")?.GetValue(obj) ??
                    t.GetProperty("CreatedAt")?.GetValue(obj);

                return v switch
                {
                    DateTime dt => dt,
#if NET9_0_OR_GREATER
                    DateOnly d => d.ToDateTime(TimeOnly.MinValue),
#endif
                    _ => (DateTime?)null
                };
            }

            var dt = PickDate(value);
            if (dt is not null) return dt;

            var sessionObj = value.GetType().GetProperty("Session")?.GetValue(value);
            if (sessionObj is not null)
                return PickDate(sessionObj);

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

   
    public class SessionDescriptionConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return null;

            var t = value.GetType();
            object? v =
                t.GetProperty("Description")?.GetValue(value) ??
                t.GetProperty("Notes")?.GetValue(value) ??
                t.GetProperty("Note")?.GetValue(value) ??
                t.GetProperty("Comment")?.GetValue(value);

            return v?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
