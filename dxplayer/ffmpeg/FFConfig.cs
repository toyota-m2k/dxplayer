using FFMpegCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.ffmpeg {
    public static class FFConfig {
        public static void SetBinaryPath(string path) {
            GlobalFFOptions.Configure(conf => {
                conf.BinaryFolder = path;
            });
        }
    }
}
