using dxplayer.data;
using dxplayer.settings;
using FFMpegCore;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.ffmpeg {
    public class FFApi {
        private static void Initialize() {
            GlobalFFOptions.Configure(conf => {
                conf.BinaryFolder = Settings.Instance.FFMpegPath;
            });
        }
        public static int MAX_LENGTH = 1920;    // 長辺の最大値
        public static int CRF = 23;             // 圧縮率


        public class ConvertResult {
            public bool Result { get; }
            public Exception Exception { get; }
            public Analyzer.Analysis Before { get; }
            public Analyzer.Analysis After { get; }
            public ConvertResult(bool result, Analyzer.Analysis before, Analyzer.Analysis after, Exception ex) {
                Result = result;
                Before = before;
                After = after;
                Exception = ex;
            }
            public static ConvertResult Error(Exception e, Analyzer.Analysis before) {
                return new ConvertResult(false, before, Analyzer.Analysis.Empty, e);
            }
            public static ConvertResult Success(Analyzer.Analysis before, Analyzer.Analysis after) {
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

        public static FFMpegArgumentProcessor CompressArguments(string inPath, string outPath, Analyzer.Analysis inputInfo) {
            Initialize();
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
                    var percent = (((double)s.Ticks * 100.0) / (double)duration.Ticks).ToString("F1");
                    Debug.WriteLine($"{s.Ticks}/{duration} ({percent}%)");
                });
            }

        public static async Task<ConvertResult> CompressAsync(string inPath, string outPath, Analyzer.Analysis inputInfo=null) {
            Initialize();
            if (inputInfo == null) {
                inputInfo = await Analyzer.AnalyzeAsync(inPath);
            }
            try {
                await CompressArguments(inPath, outPath, inputInfo)
                .ProcessAsynchronously();
                return ConvertResult.Success(inputInfo, await Analyzer.AnalyzeAsync(outPath));
            }
            catch (Exception e) {
                return ConvertResult.Error(e, inputInfo);
            }

        }
        public static ConvertResult Compress(string inPath, string outPath, Analyzer.Analysis inputInfo = null) {
            Initialize();
            if (inputInfo == null) {
                inputInfo = Analyzer.Analyze(inPath);
            }
            try {
                CompressArguments(inPath, outPath, inputInfo)
                .ProcessSynchronously();
                return ConvertResult.Success(inputInfo, Analyzer.Analyze(outPath));
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

        public static ConvertResult SimpleTrimming(string inPath, string outPath, PlayRange range, Analyzer.Analysis inputInfo) {
            Initialize();
            if (inputInfo == null) {
                inputInfo = Analyzer.Analyze(inPath);
            }
            try {
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
                return ConvertResult.Success(inputInfo, Analyzer.Analyze(outPath));
            }
            catch (Exception e) {
                PathUtil.safeDeleteFile(outPath);
                return ConvertResult.Error(e, inputInfo);
            }
        }

        public static ConvertResult Trimming(string inPath, string outPath, List<PlayRange> ranges, Analyzer.Analysis inputInfo) {
            Initialize();
            if (inputInfo == null) {
                inputInfo = Analyzer.Analyze(inPath);
            }
            if(ranges.Count == 0) {
                return ConvertResult.Error(new Exception("No range specified"), inputInfo);
            }
            if(ranges.Count == 1) {
                return SimpleTrimming(inPath, outPath, ranges[0], inputInfo);
            }

            var tempFiles = new List<string>();
            var listFile = TempPath(outPath, "list.txt");
            try {
                for(int i = 0; i < ranges.Count; i++) {
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
                    Debug.WriteLine($"Splitting: {range} -> {tempPath}");
                }

                FFMpegArguments.FromDemuxConcatInput(tempFiles)
                    .OutputToFile(outPath, overwrite: true)
                    .NotifyOnProgress((TimeSpan s) => {
                        Debug.WriteLine($"Combining: {s}");
                    })
                    .Apply(it => {
                        Debug.WriteLine(it.Arguments);
                    })
                    .ProcessSynchronously();

                return ConvertResult.Success(inputInfo, Analyzer.Analyze(outPath));
            }
            catch (Exception e) {
                PathUtil.safeDeleteFile(outPath);
                return ConvertResult.Error(e, inputInfo);
            }
            finally {
                foreach(var file in tempFiles) {
                    PathUtil.safeDeleteFile(file);
                }
                PathUtil.safeDeleteFile(listFile);
            }
        }
    }
}
