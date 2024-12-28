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
using static dxplayer.ffmpeg.FFApi;

namespace dxplayer.ffmpeg {
    public static class FFApi {
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
                    .Append("-------------------------------------\r\n")
                    .Append(After.ToString())
                    .Append("-------------------------------------\r\n")
                    .ToString();
            }
        }

        private static Size? calcVideoSizeForCompress(FFAnalyzer.Analysis inputInfo) {
            var video = inputInfo.Video;
            if (video.IsEmpty) {
                return null;
            }

            if (video.Width > video.Height) {
                if (video.Width > MAX_LENGTH) {
                    return new Size(MAX_LENGTH, -1);
                }
            }
            else {
                if (video.Height > MAX_LENGTH) {
                    return new Size(-1, MAX_LENGTH);
                }
            }
            return null;
        }

        public static FFMpegArgumentProcessor CompressArguments(string inPath, string outPath, IFFProgress progress, FFAnalyzer.Analysis inputInfo) {
            FFConfig.Configure();
            Size? size = calcVideoSizeForCompress(inputInfo);
            var duration = inputInfo.Duration;

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
                    TimeSpanProgress("Compressing", progress, s, duration);
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

        public enum ExtractOption {
            QUICK,              // -c copy
            ACCURATE,
            ACCURATE_COMPRESS,  // -crf CRF -vf scale ...
        }

        private static FFMpegArgumentProcessor ExtractArguments(string inPath, string outPath, PlayRange range, ExtractOption extractOption, IFFProgress progress, FFAnalyzer.Analysis inputInfo, TimeSpan initial, TimeSpan total) {
            return FFMpegArguments
                    .FromFileInput(inPath, verifyExists: true, (options) => {
                        options.Seek(TimeSpan.FromMilliseconds(range.Start));
                        if (range.End != 0 && range.End > range.Start) {
                            options.WithDuration(TimeSpan.FromMilliseconds(range.End - range.Start));
                        }
                    })
                    .OutputToFile(outPath, overwrite: true, (options) => {
                        switch (extractOption) {
                            case ExtractOption.QUICK:
                                options.CopyChannel();
                                break;
                            case ExtractOption.ACCURATE_COMPRESS:
                                Size? size = calcVideoSizeForCompress(inputInfo);
                                if (size.HasValue) {
                                    options
                                    .WithVideoFilters(filter => {
                                        filter
                                        .Scale(size.Value);
                                    });
                                }
                                options
                                .WithConstantRateFactor(CRF)
                                .WithFastStart();
                                break;
                            default:    // ExtractOption.ACCURATE
                                break;
                        }
                    })
                    .NotifyOnProgress(ts => {
                        TimeSpanProgress("Extracting", progress, ts + initial, total);
                    })
                    .Apply(it => {
                        Debug.WriteLine(it.Arguments);
                    });
        }

        private static ConvertResult Extract(string inPath, string outPath, PlayRange range, ExtractOption extractOption, IFFProgress progress, TimeSpan initial, TimeSpan total, FFAnalyzer.Analysis inputInfo) {
            if (inputInfo == null) {
                inputInfo = FFAnalyzer.Analyze(inPath);
            }
            try {
                ExtractArguments(inPath, outPath, range, extractOption, progress, inputInfo, initial, total)
                    .ProcessSynchronously();
                return ConvertResult.Success(inputInfo, FFAnalyzer.Analyze(outPath));
            }
            catch (Exception e) {
                PathUtil.safeDeleteFile(outPath);
                return ConvertResult.Error(e, inputInfo);
            }
        }
        private static async Task<ConvertResult> ExtractAsync(string inPath, string outPath, PlayRange range, ExtractOption extractOption, IFFProgress progress, TimeSpan initial, TimeSpan total, FFAnalyzer.Analysis inputInfo) {
            if (inputInfo == null) {
                inputInfo = FFAnalyzer.Analyze(inPath);
            }
            try {
                await ExtractArguments(inPath, outPath, range, extractOption, progress, inputInfo, initial, total)
                    .ProcessAsynchronously();
                return ConvertResult.Success(inputInfo, FFAnalyzer.Analyze(outPath));
            }
            catch (Exception e) {
                PathUtil.safeDeleteFile(outPath);
                return ConvertResult.Error(e, inputInfo);
            }
        }

        //public static ConvertResult SimpleTrimming(string inPath, string outPath, PlayRange range, IFFProgress progress, FFAnalyzer.Analysis inputInfo) {
        //    FFConfig.Configure();
        //    if (inputInfo == null) {
        //        ProgressProcess(progress, FFProcessId.ANALYZING);
        //        inputInfo = FFAnalyzer.Analyze(inPath);
        //    }
        //    try {
        //        ProgressProcess(progress, FFProcessId.TRIMMING);
        //        SimpleTrimmingArguments(inPath, outPath, range)
        //            .ProcessSynchronously();
        //        return ConvertResult.Success(inputInfo, FFAnalyzer.Analyze(outPath));
        //    }
        //    catch (Exception e) {
        //        PathUtil.safeDeleteFile(outPath);
        //        return ConvertResult.Error(e, inputInfo);
        //    }
        //}
        //public static async Task<ConvertResult> SimpleTrimmingAsync(string inPath, string outPath, PlayRange range, IFFProgress progress, FFAnalyzer.Analysis inputInfo) {
        //    FFConfig.Configure();
        //    if (inputInfo == null) {
        //        ProgressProcess(progress, FFProcessId.ANALYZING);
        //        inputInfo = FFAnalyzer.Analyze(inPath);
        //    }
        //    try {
        //        ProgressProcess(progress, FFProcessId.TRIMMING);
        //        await SimpleTrimmingArguments(inPath, outPath, range)
        //            .ProcessAsynchronously();
        //        return ConvertResult.Success(inputInfo, FFAnalyzer.Analyze(outPath));
        //    }
        //    catch (Exception e) {
        //        PathUtil.safeDeleteFile(outPath);
        //        return ConvertResult.Error(e, inputInfo);
        //    }
        //}

        
        private static FFMpegArgumentProcessor CombineArguments(List<string> files, string outPath, IFFProgress progress, TimeSpan? totalDuration) {
            return FFMpegArguments.FromDemuxConcatInput(files)
                .OutputToFile(outPath, overwrite:true, (options) => {
                    options.CopyChannel();
                })
                .NotifyOnProgress((TimeSpan s) => {
                    if (totalDuration.HasValue) {
                        TimeSpanProgress("Combining", progress, s, totalDuration.Value);
                    } else {
                        Debug.WriteLine($"Combining: {TimeSpanText(s)}");
                        if (progress != null) {
                            progress.Percent = 0;
                            progress.ProgressMessage = TimeSpanText(s);
                        }
                    }
                })
                .Apply(it => {
                    Debug.WriteLine(it.Arguments);
                });
        }

        public static ConvertResult Combine(List<string> files, string outPath, IFFProgress progress, TimeSpan? totalDuration) {
            try {
                ProgressProcess(progress, FFProcessId.COMBINING);
                CombineArguments(files, outPath, progress, totalDuration)
                    .ProcessSynchronously();
                return ConvertResult.Success(FFAnalyzer.Analysis.Empty, FFAnalyzer.Analyze(outPath));
            } catch(Exception e) {
                PathUtil.safeDeleteFile(outPath);
                return ConvertResult.Error(e, FFAnalyzer.Analyze(outPath));
            }
        }
        public static async Task<ConvertResult> CombineAsync(List<string> files, string outPath, IFFProgress progress, TimeSpan? totalDuration) {
            try {
                ProgressProcess(progress, FFProcessId.COMBINING);
                await CombineArguments(files, outPath, progress, totalDuration)
                    .ProcessAsynchronously();
                return ConvertResult.Success(FFAnalyzer.Analysis.Empty, FFAnalyzer.Analyze(outPath));
            }
            catch (Exception e) {
                PathUtil.safeDeleteFile(outPath);
                return ConvertResult.Error(e, FFAnalyzer.Analyze(outPath));
            }
        }

        private static string TimeSpanText(TimeSpan ts) {
            return ts.ToString(@"hh\:mm\:ss");
        }
        private static void TimeSpanProgress(string label, IFFProgress progress, TimeSpan current, TimeSpan total) {
            string currentText, totalText;
            if(total.TotalMinutes > 60) {
                currentText = current.ToString(@"hh\:mm\:ss");
                totalText = total.ToString(@"hh\:mm\:ss");
            } else {
                currentText = current.ToString(@"mm\:ss");
                totalText = total.ToString(@"mm\:ss");
            }
            var percent = (total.Ticks>0) ? Math.Min(100, (double)current.Ticks * 100 / (double)total.Ticks) : 0;
            var percentText = percent.ToString("F1");
            var progressMessage = $"{currentText}/{totalText} ({percentText}%)";
            Debug.WriteLine($"{label}: {progressMessage}");
            if (progress != null) {
                progress.Percent = percent;
                progress.ProgressMessage = progressMessage;
            }
        }
        private static void CountProgress(string label, IFFProgress progress, int current, int total) {
            var percent = (double)current * 100 / (double)total;
            var progressMessage = $"{current}/{total} ({Math.Round(percent)}%)";
            Debug.WriteLine($"{label}: {progressMessage}");
            if (progress != null) {
                progress.Percent = percent;
                progress.ProgressMessage = progressMessage;
            }
        }

        public static ConvertResult Trimming(string inPath, string outPath, List<PlayRange> ranges, ExtractOption extractOption, IFFProgress progress, FFAnalyzer.Analysis inputInfo) {
            FFConfig.Configure();
            if (inputInfo == null) {
                inputInfo = FFAnalyzer.Analyze(inPath);
            }
            if(ranges.Count == 0) {
                return ConvertResult.Error(new Exception("No range specified"), inputInfo);
            }
            if(ranges.Count == 1) {
                ProgressProcess(progress, FFProcessId.TRIMMING);
                return Extract(inPath, outPath, ranges[0], extractOption, progress, TimeSpan.Zero, TimeSpan.FromMilliseconds(ranges[0].TrueSpan((ulong)inputInfo.DurationMs)), inputInfo);
            }

            var totalDuration = TimeSpan.FromMilliseconds(ranges.Aggregate((ulong)0, (sum, range) => sum + range.TrueSpan((ulong)inputInfo.DurationMs)));
            var tempFiles = new List<string>();
            try {
                // PlayRange毎にファイルを分割
                ProgressProcess(progress, FFProcessId.SPLITTING);
                TimeSpan initial = TimeSpan.Zero;
                for (int i = 0; i < ranges.Count; i++) {
                    var tempPath = TempPathFrom(outPath, i);
                    tempFiles.Add(tempPath);
                    var range = ranges[i];
                    initial += TimeSpan.FromMilliseconds(range.TrueSpan((ulong)inputInfo.DurationMs));
                    if (!Extract(inPath, tempPath, range, extractOption, progress, initial, totalDuration, inputInfo).Result) {
                        throw new Exception($"Failed to split {i}");
                    }
                    //CountProgress("Splitting:", progress, i + 1, ranges.Count);
                }

                // 分割したファイルを結合
                var combineResult = Combine(tempFiles, outPath, progress, totalDuration);
                if(!combineResult.Result) {
                    throw new Exception("Failed to combine files.");
                }

                ProgressProcess(progress, FFProcessId.DISPOSING, 100, "Completed.");
                return ConvertResult.Success(inputInfo, combineResult.After);
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
            }
        }
        public static async Task<ConvertResult> TrimmingAsync(string inPath, string outPath, List<PlayRange> ranges, ExtractOption extractOption, IFFProgress progress, FFAnalyzer.Analysis inputInfo) {
            FFConfig.Configure();
            if (inputInfo == null) {
                inputInfo = FFAnalyzer.Analyze(inPath);
            }
            if (ranges.Count == 0) {
                return ConvertResult.Error(new Exception("No range specified"), inputInfo);
            }
            if (ranges.Count == 1) {
                ProgressProcess(progress, FFProcessId.TRIMMING);
                return await ExtractAsync(inPath, outPath, ranges[0], extractOption, progress, TimeSpan.Zero, TimeSpan.FromMilliseconds(ranges[0].TrueSpan((ulong)inputInfo.DurationMs)), inputInfo);
            }

            var totalDuration = TimeSpan.FromMilliseconds(ranges.Aggregate((ulong)0, (sum, range) => sum + range.TrueSpan((ulong)inputInfo.DurationMs)));
            var tempFiles = new List<string>();
            try {
                // PlayRange毎にファイルを分割
                ProgressProcess(progress, FFProcessId.SPLITTING);
                TimeSpan initial = TimeSpan.Zero;
                for (int i = 0; i < ranges.Count; i++) {
                    var tempPath = TempPathFrom(outPath, i);
                    tempFiles.Add(tempPath);
                    var range = ranges[i];
                    initial += TimeSpan.FromMilliseconds(range.TrueSpan((ulong)inputInfo.DurationMs));
                    if (!(await ExtractAsync(inPath, tempPath, range, extractOption, progress, initial, totalDuration, inputInfo)).Result) {
                        throw new Exception($"Failed to split {i}");
                    }
                    // CountProgress("Splitting:", progress, i + 1, ranges.Count);
                }

                // 分割したファイルを結合
                var combineResult = await CombineAsync(tempFiles, outPath, progress, totalDuration);
                if (!combineResult.Result) {
                    throw new Exception("Failed to combine files.");
                }

                ProgressProcess(progress, FFProcessId.DISPOSING, 100, "Completed.");
                return ConvertResult.Success(inputInfo, combineResult.After);
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
            }
        }
    }
}
