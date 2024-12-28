using FFMpegCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.ffmpeg {
    public static class FFConfig {
        private static string FFMpegPath = null;
        private static Func<string> FFMpegPathResolver { get; set; } = null;
        private const int DEFAULT_MAX_LENGTH = 1440;
        private const int DEFAULT_MAX_FPS = 30;
        public static int MaxLengthInPixel { get; private set; } = DEFAULT_MAX_LENGTH /*HD*/; // 1920 FHD;
        public static int MaxFrameRate { get; private set; } = DEFAULT_MAX_FPS;
        public static int CRF { get; private set; } = 23;
        /**
         * FFMpegPathを取得するための関数を設定します。
         * Settingsなどから設定値を取得するデリゲートを設定しておけば、設定が変更されるたびに呼び出す必要がありません。
         * FFMpegPathと両方を指定した場合は、FFMpegPathResolverが優先されます。
         */
        public static void Initialize(Func<string> ffmpegPathResolver, int maxLengthInPixel= DEFAULT_MAX_LENGTH, int maxFrameRate=DEFAULT_MAX_FPS, int crf=23) {
            FFMpegPathResolver = ffmpegPathResolver;
            MaxLengthInPixel = maxLengthInPixel;
            MaxFrameRate = maxFrameRate;
            CRF = crf;
        }
        /**
         * FFMpegPathを文字列で設定します。
         * Settingsなどで変更した場合に呼び出す必要があります。
         */
        public static void SetFFMpegPath(string ffmpegPath) {
            FFMpegPath= ffmpegPath;
        }
        /**
         * FSApiからFFMpegCoreを初期化するために呼び出します。
         */
        public static void Configure() {
            GlobalFFOptions.Configure(conf => {
                conf.BinaryFolder = FFMpegPathResolver?.Invoke() ?? FFMpegPath;
            });
        }
    }
}
