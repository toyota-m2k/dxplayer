using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.ffmpeg {
    public enum FFProcessId {
        NONE,
        ANALYZING,
        TRIMMING,
        SPLITTING,
        COMBINING,
        COMPRESSING,
        DISPOSING,
    }
    public interface IFFProgress {
        FFProcessId ProcessId { get; set; } // 現在の処理内容
        double Percent { get; set; } // 進捗率
        string ProgressMessage { get; set; } // 進捗メッセージ (例: "1/10 (10%)")
    }
}
