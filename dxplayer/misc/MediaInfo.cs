using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using io.github.toyota32k.toolkit.utils;

namespace dxplayer.misc {
    public static class MediaInfo {
        public static TimeSpan? GetDuration(string filePath) {
            try {
                using (ShellObject Shell = ShellObject.FromParsingName(filePath)) {
                    var ticks = Shell.Properties.System.Media.Duration.Value;
                    if (ticks.HasValue) {
                        return TimeSpan.FromTicks((long)ticks);
                    }
                    return null;
                }
            } catch(Exception e) {
                LoggerEx.error(e);
                return null;
            }
        }
    }
}
