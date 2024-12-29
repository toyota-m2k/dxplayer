using FFMpegCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace dxplayer.ffmpeg {
    public class FFAnalyzer {
        public interface IMediaInfo {
            bool IsEmpty { get; }
            string CodecName { get; }
            string Profile { get; }
            TimeSpan Duration { get; }
            long BitRate { get; }
        }
        public interface IVideoInfo : IMediaInfo {
            int Width { get; }
            int Height { get; }
            double FrameRate { get; }
        }
        public interface IAudioInfo : IMediaInfo {
            int Channels { get; }
            int SampleRate { get; }
        }

        private abstract class MediaInfo : IMediaInfo {
            public string CodecName { get; }
            public string Profile { get; }
            public TimeSpan Duration { get; }
            public long BitRate { get; }
            public bool IsEmpty => string.IsNullOrEmpty(CodecName);
            protected MediaInfo() {
                CodecName = "";
                Profile = "";
                Duration = TimeSpan.Zero;
                BitRate = 0;
            }
            protected MediaInfo(string codecName, string profile, TimeSpan duration, long bitRate) {
                CodecName = codecName;
                Profile = profile;
                Duration = duration;
                BitRate = bitRate;
            }
            public override string ToString() {
                StringBuilder sb = new StringBuilder();
                sb.Append("Codec     :").Append(CodecName).Append("\r\n");
                sb.Append("Profile   :").Append(Profile).Append("\r\n");
                sb.Append("Duration  :").Append(Duration).Append("\r\n");
                sb.Append("BitRate   :").Append(BitRate.ToString("N0", CultureInfo.InvariantCulture)).Append("\r\n");
                return sb.ToString();
            }
        }
        private class AudioInfo : MediaInfo, IAudioInfo {
            public int Channels { get; }
            public int SampleRate { get; }

            private AudioInfo() {
                Channels = 0;
                SampleRate = 0;
            }
            private AudioInfo(string codecName, string profile, TimeSpan duration, long bitRate, int channels, int sampleRate)
                : base(codecName, profile, duration, bitRate) {
                Channels = channels;
                SampleRate = sampleRate;
            }
            public static AudioInfo Empty { get; } = new AudioInfo();
            public static AudioInfo FromProbe(IMediaAnalysis m) {
                var a = m.PrimaryAudioStream;
                if (a == null) return Empty;
                return new AudioInfo(a.CodecName, a.Profile, a.Duration, a.BitRate, a.Channels, a.SampleRateHz);
            }
            public override string ToString() {
                if (IsEmpty) return "No Audio Stream";
                StringBuilder sb = new StringBuilder();
                sb.Append("■ Audio").Append("\r\n");
                sb.Append(base.ToString());
                sb.Append("Channels  :").Append(Channels).Append("\r\n");
                sb.Append("SampleRate:").Append(SampleRate).Append("\r\n");
                return sb.ToString();
            }
        }

        private class VideoInfo : MediaInfo, IVideoInfo {
            public int Width { get; }
            public int Height { get; }
            public double FrameRate { get; }

            private VideoInfo() {
                Width = 0;
                Height = 0;
                FrameRate = 0;
            }
            private VideoInfo(string codecName, string profile, TimeSpan duration, long bitRate, int width, int height, double frameRate)
                : base(codecName, profile, duration, bitRate) {
                Width = width;
                Height = height;
                FrameRate = frameRate;
            }
            public static VideoInfo Empty { get; } = new VideoInfo();
            public static VideoInfo FromProbe(IMediaAnalysis m) {
                var v = m.PrimaryVideoStream;
                if (v == null) return Empty;
                return new VideoInfo(v.CodecName, v.Profile, v.Duration, v.BitRate, v.Width, v.Height, v.FrameRate);
            }
            public override string ToString() {
                if (IsEmpty) return "No Video Stream";
                StringBuilder sb = new StringBuilder();
                sb.Append("■ Video").Append("\r\n");
                sb.Append(base.ToString());
                sb.Append("Width     :").Append(Width).Append("\r\n");
                sb.Append("Height    :").Append(Height).Append("\r\n");
                sb.Append("FrameRate :").Append(FrameRate).Append("\r\n");
                return sb.ToString();
            }
        }


        public class Analysis {
            public long Size { get; }
            public IVideoInfo Video { get; }
            public IAudioInfo Audio { get; }
            public bool IsEmpty => Video.IsEmpty && Audio.IsEmpty;
            public ulong DurationMs {
                get {
                    if(Video!=null && Audio!=null) {
                        return Math.Max((ulong)Video.Duration.TotalMilliseconds, (ulong)Audio.Duration.TotalMilliseconds);
                    } else {
                        return (ulong)(Video?.Duration.TotalMilliseconds ?? Audio?.Duration.TotalMilliseconds ?? 0);
                    }
                }
            }
            public TimeSpan Duration => TimeSpan.FromMilliseconds(DurationMs);

            private Analysis(long size, IVideoInfo video, IAudioInfo audio) {
                Size = size;
                Video = video;
                Audio = audio;
            }
            public static Analysis FromPath(string path) {
                var fi = new FileInfo(path);
                try {
                    var m = FFProbe.Analyse(path);
                    return new Analysis(fi.Length, VideoInfo.FromProbe(m), AudioInfo.FromProbe(m));
                } catch (Exception e) {
                    Debug.WriteLine(e.Message);
                    return Empty;
                }
            }
            public static async Task<Analysis> FromPathAsync(string path) {
                var fi = new FileInfo(path);
                var m = await FFProbe.AnalyseAsync(path);
                return new Analysis(fi.Length, VideoInfo.FromProbe(m), AudioInfo.FromProbe(m));
            }

            public override string ToString() {
                StringBuilder sb = new StringBuilder();
                sb.Append("Size:     :").Append(Size.ToString("N0", CultureInfo.InvariantCulture)).Append("\r\n");
                sb.Append("-------------------r\n");
                sb.Append(Video.ToString());
                sb.Append("-------------------r\n");
                sb.Append(Audio.ToString());
                return sb.ToString();
            }
            public static Analysis Empty => new Analysis(0, VideoInfo.Empty, AudioInfo.Empty);
        }

        public static Analysis Analyze(string path) {
            FFConfig.Configure();
            return Analysis.FromPath(path);
        }
        public static async Task<Analysis> AnalyzeAsync(string path) {
            FFConfig.Configure();
            return await Analysis.FromPathAsync(path);
        }
    }
}
