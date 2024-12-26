using FFMpegCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.ffmpeg {
    public static class FFConfig {
        static FFConfig() {
            SetBinaryPath("c:\\bin\\tools");
        }
        public static void SetBinaryPath(string path) {
            GlobalFFOptions.Configure(conf => {
                conf.BinaryFolder = path;
            });
        }
    }
}
