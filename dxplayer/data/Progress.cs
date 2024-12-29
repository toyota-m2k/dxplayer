using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.data {
    public interface IProgressEntity {
        string Title { get; }
        double Percent { get; }
        string ProgressText { get; }
    }
    public class ProgressEntity<T> : IProgressEntity {
        public string Title { get; set; }
        private Func<T, string> mToText { get; } = null;
        private Func<T, double> mToDouble{ get; } = null;

        public T Current;
        public T Total;

        public double Percent =>  mToDouble(Total) == 0 ? 0 : (mToDouble(Current) / mToDouble(Total))*100;
        public string ProgressText => $"{mToText(Current)} / {mToText(Total)} ({Percent:0.0}%)";

        public ProgressEntity(string title, T current, T total, Func<T, string> toText, Func<T, double> toDouble) {
            Title = title;
            Current = current;
            Total = total;
            mToText = toText;
            mToDouble = toDouble;
        }
    }



    public class Progress {
        public int Current { get; set; }
        public int Total { get; set; }
        public string Message { get; set; }
        public Progress(int current, int total, string message) {
            Current = current;
            Total = total;
            Message = message;
        }
    }
}
