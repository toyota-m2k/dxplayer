using dxplayer.data;    // for PlayRange
using FFMpegCore;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.ffmpeg {
    public class FFApi {
        public static int MAX_LENGTH => FFConfig.MaxLengthInPixel;    // 長辺の最大値
        public static int CRF = FFConfig.CRF;                         // 圧縮率


        public class ConvertResult {
            public bool Result { get; }
            public Exception Exception { get; }
            public FFAnalyzer.Analysis Before { get; }
            public FFAnalyzer.Analysis After { get; }
            public ConvertResult(bool result, FFAnalyzer.Analysis before, FFAnalyzer.Analysis after, Exception ex) {
                Result = result;
                Before = before;
                After = after;
                Exception = ex;
            }
            public static ConvertResult Error(Exception e, FFAnalyzer.Analysis before) {
                return new ConvertResult(false, before, FFAnalyzer.Analysis.Empty, e);
            }
            public static ConvertResult Success(FFAnalyzer.Analysis before, FFAnalyzer.Analysis after) {
                return new ConvertResult(true, before, after, null);
            }
            public override string ToString() {
                var sb = new StringBuilder();
                return sb
                    .Append("Convert Result:").Append("\r\n")
                    .Append("Result: ").Append(Result).Append("\r\n")
                    .Append("Size: ")
                    .Append(Before.Size.ToString("N0", CultureInfo.InvariantCulture))
                    .Append(" --> ")
                    .Append(After.Size.ToString("N0", CultureInfo.InvariantCulture))
                    .Append("\r\n")
                    .Append(Before.ToString())
                    .Append(After.ToString())
                    .ToString();
            }
        }

        public static FFMpegArgumentProcessor CompressArguments(string inPath, string outPath, IFFProgress progress, FFAnalyzer.Analysis inputInfo) {
            FFConfig.Configure();
            var video = inputInfo.Video;
            if (video.IsEmpty) {
                throw new Exception("No video stream found");
            }

            Size? size = null;
            if (video.Width > video.Height) {
                if (video.Width > MAX_LENGTH) {
                    size = new Size(MAX_LENGTH, -1);
                }
            }
            else {
                if (video.Height > MAX_LENGTH) {
                    size = new Size(-1, MAX_LENGTH);
                }
            }
            var duration = video.Duration;

            return FFMpegArguments
                .FromFileInput(inPath)
                .OutputToFile(outPath, overwrite: false, options => {
                    if (size.HasValue) {
                        options
                        .WithVideoFilters(filter => {
                            filter
                            .Scale(size.Value);
                        });
                    }
                    options
                    .WithConstantRateFactor(CRF)
                    .WithFastStart()
                    ;
                })
                .NotifyOnProgress((TimeSpan s) => {
                    var percent = ((double)s.Ticks * 100.0) / (double)duration.Ticks;
                    var percentText = percent.ToString("F1");
                    var progressMessage = ProgressTimeSpanText(s, duration);
                    Debug.WriteLine($"Compressing: {progressMessage}");
                    if(progress!=null) {
                        progress.Percent = percent;
                        progress.ProgressMessage = progressMessage;
                    }
                });
            }

        private static void ProgressProcess(IFFProgress progress, FFProcessId id, double percent=0, string message="") {
            if (progress != null) {
                progress.ProcessId = id;
                progress.Percent = percent;
                progress.ProgressMessage = message;
            }
        }

        public static async Task<ConvertResult> CompressAsync(string inPath, string outPath, IFFProgress progress, FFAnalyzer.Analysis inputInfo=null) {
            FFConfig.Configure();
            if (inputInfo == null) {
                ProgressProcess(progress,FFProcessId.ANALYZING);
                inputInfo = await FFAnalyzer.AnalyzeAsync(inPath);
            }
            try {
                ProgressProcess(progress, FFProcessId.COMPRESSING);
                await CompressArguments(inPath, outPath, progress, inputInfo)
                .ProcessAsynchronously();
                return ConvertResult.Success(inputInfo, await FFAnalyzer.AnalyzeAsync(outPath));
            }
            catch (Exception e) {
                return ConvertResult.Error(e, inputInfo);
            }
        }
        public static ConvertResult Compress(string inPath, string outPath, IFFProgress progress, FFAnalyzer.Analysis inputInfo = null) {
            FFConfig.Configure();
            if (inputInfo == null) {
                ProgressProcess(progress, FFProcessId.ANALYZING);
                inputInfo = FFAnalyzer.Analyze(inPath);
            }
            try {
                ProgressProcess(progress, FFProcessId.COMPRESSING);
                CompressArguments(inPath, outPath, progress, inputInfo)
                .ProcessSynchronously();
                return ConvertResult.Success(inputInfo, FFAnalyzer.Analyze(outPath));
            }
            catch (Exception e) {
                PathUtil.safeDeleteFile(outPath);
                return ConvertResult.Error(e, inputInfo);
            }
        }

        static private string TempPathFrom(string path, int num) {
            var dir = Path.GetDirectoryName(path);
            var ext = Path.GetExtension(path);
            var name = Path.GetFileNameWithoutExtension(path);
            return Path.Combine(dir, $"{name}_{num}{ext}");
        }
        static private string TempPath(string path, string name) {
            var dir = Path.GetDirectoryName(path);
            var ext = Path.GetExtension(path);
            return Path.Combine(dir, name);
        }

        public static ConvertResult SimpleTrimming(string inPath, string outPath, PlayRange range, IFFProgress progress, FFAnalyzer.Analysis inputInfo) {
            FFConfig.Configure();
            if (inputInfo == null) {
                ProgressProcess(progress, FFProcessId.ANALYZING);
                inputInfo = FFAnalyzer.Analyze(inPath);
            }
            try {
                ProgressProcess(progress, FFProcessId.TRIMMING);
                FFMpegArguments
                    .FromFileInput(inPath, verifyExists: true, (options) => {
                        options.Seek(TimeSpan.FromMilliseconds(range.Start));
                        if (range.End != 0 && range.End > range.Start) {
                            options.WithDuration(TimeSpan.FromMilliseconds(range.End - range.Start));
                        }
                    })
                    .OutputToFile(outPath, overwrite: true, (options) => {
                        options.CopyChannel();
                    })
                    .Apply(it => {
                        Debug.WriteLine(it.Arguments);
                    })
                    .ProcessSynchronously();
                return ConvertResult.Success(inputInfo, FFAnalyzer.Analyze(outPath));
            }
            catch (Exception e) {
                PathUtil.safeDeleteFile(outPath);
                return ConvertResult.Error(e, inputInfo);
            }
        }

        //private static string TimeSpanText(TimeSpan ts) {
        //    return ts.ToString(@"hh\:mm\:ss");
        //}
        private static string ProgressTimeSpanText(TimeSpan current, TimeSpan total) {
            string currentText, totalText;
            if(total.TotalMinutes > 60) {
                currentText = current.ToString(@"hh\:mm\:ss");
                totalText = total.ToString(@"hh\:mm\:ss");
            } else {
                currentText = current.ToString(@"mm\:ss");
                totalText = total.ToString(@"mm\:ss");
            }
            return $"{currentText}/{totalText}";
        }

        public static ConvertResult Trimming(string inPath, string outPath, List<PlayRange> ranges, IFFProgress progress, FFAnalyzer.Analysis inputInfo) {
            FFConfig.Configure();
            if (inputInfo == null) {
                ProgressProcess(progress, FFProcessId.ANALYZING);
                inputInfo = FFAnalyzer.Analyze(inPath);
            }
            if(ranges.Count == 0) {
                return ConvertResult.Error(new Exception("No range specified"), inputInfo);
            }
            if(ranges.Count == 1) {
                return SimpleTrimming(inPath, outPath, ranges[0], progress, inputInfo);
            }

            var tempFiles = new List<string>();
            var listFile = TempPath(outPath, "list.txt");
            try {
                ProgressProcess(progress, FFProcessId.SPLITTING);
                for (int i = 0; i < ranges.Count; i++) {
                    var tempPath = TempPathFrom(outPath, i);
                    tempFiles.Add(tempPath);
                    var range = ranges[i];

                    FFMpegArguments
                        .FromFileInput(inPath, verifyExists: true, (options) => {
                            options.Seek(TimeSpan.FromMilliseconds(range.Start));
                            if (range.End != 0 && range.End > range.Start) {
                                options.WithDuration(TimeSpan.FromMilliseconds(range.End - range.Start));
                            }
                        })
                        .OutputToFile(tempPath, overwrite: true, (options) => {
                            options.CopyChannel();
                        })
                        .Apply(it => {
                            Debug.WriteLine(it.Arguments);
                        })
                        .ProcessSynchronously();

                    var percent = (double)(i + 1) * 100 / (double)ranges.Count;
                    var progressMessage = $"{i + 1}/{ranges.Count} ({Math.Round(percent)}%)";
                    Debug.WriteLine($"Splitting: {progressMessage}");
                    if (progress != null) {
                        progress.Percent = percent;
                        progress.ProgressMessage = progressMessage;
                    }
                }

                var totalDuration = TimeSpan.FromMilliseconds(ranges.Aggregate((ulong)0, (sum, range) => sum + range.TrueSpan((ulong)inputInfo.Video.Duration.TotalMilliseconds)));
                ProgressProcess(progress, FFProcessId.COMBINING);
                FFMpegArguments.FromDemuxConcatInput(tempFiles)
                    .OutputToFile(outPath, overwrite: true)
                    .NotifyOnProgress((TimeSpan s) => {
                        var percent = (double)s.Ticks * 100 / (double)totalDuration.Ticks;
                        var percentText = percent.ToString("F1");
                        var progressMessage = ProgressTimeSpanText(s, totalDuration);
                        Debug.WriteLine($"Combining: {progressMessage}");
                        if(progress != null) {
                            progress.Percent = percent;
                            progress.ProgressMessage = progressMessage;
                        }
                    })
                    .Apply(it => {
                        Debug.WriteLine(it.Arguments);
                    })
                    .ProcessSynchronously();

                ProgressProcess(progress, FFProcessId.DISPOSING, 100, "Completed.");
                return ConvertResult.Success(inputInfo, FFAnalyzer.Analyze(outPath));
            }
            catch (Exception e) {
                ProgressProcess(progress, FFProcessId.DISPOSING, 0, "Failed.");
                PathUtil.safeDeleteFile(outPath);
                return ConvertResult.Error(e, inputInfo);
            }
            finally {
                foreach (var file in tempFiles) {
                    PathUtil.safeDeleteFile(file);
                }
                PathUtil.safeDeleteFile(listFile);
            }
        }
    }
}
