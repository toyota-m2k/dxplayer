using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.data {
    public interface IStatusBar {
        void OutputStatusMessage(string msg);
        void FlashStatusMessage(string msg, int duration = 5/*sec*/);
    }
}
