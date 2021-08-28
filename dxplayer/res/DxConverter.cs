using dxplayer.data.main;
using System;
using System.Globalization;
using System.Windows.Data;

namespace dxplayer.res
{
    public class AspectStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            switch (System.Convert.ToInt32(value)) {
                case (int)Aspect.CUSTOM125:
                    return "5:4";
                case (int)Aspect.CUSTOM133:
                    return "4:3";
                case (int)Aspect.CUSTOM150:
                    return "3:2";
                case (int)Aspect.CUSTOM177:
                    return "16:9";
                default:
                    return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class DurationStringConverter : IValueConverter {
        private string FormatDuration(ulong durationInSec) {
            var t = TimeSpan.FromMilliseconds(durationInSec);
            if (t.Hours > 0) {
                return string.Format("{0}:{1:00}.{2:00}", t.Hours, t.Minutes, t.Seconds);
            } else {
                return string.Format("{0:00}.{1:00}", t.Minutes, t.Seconds);
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return FormatDuration(System.Convert.ToUInt64(value));
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class DateStringConverter : IValueConverter {
        private readonly DateTime EpochDate = new DateTime(1970, 1, 1, 0, 0, 0);
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is DateTime) {
                if (((DateTime)value)>EpochDate) {
                    return ((DateTime)value).ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

}
