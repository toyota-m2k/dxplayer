using dxplayer.data.main;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace dxplayer.res {
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
}
