using dxplayer.data.dxx;
using dxplayer.data.wf;
using dxplayer.ffmpeg;
using dxplayer.misc;
using dxplayer.player;
using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static dxplayer.ffmpeg.FFApi;

namespace dxplayer.data.main
{
    public enum Rating {
        DREADFUL = 1,
        BAD = 2,
        NORMAL = 3,
        GOOD = 4,
        EXCELLENT = 5,
    }
    public enum Aspect {
        AUTO = 0,
        CUSTOM125,      // 5:4
        CUSTOM133,      // 4:3
        CUSTOM150,      // 3:2
        CUSTOM177,      // 16:9
    }

    [Table(Name = "t_play_list")]
    public class PlayItem : PropertyChangeNotifier, IPlayItem {
        [Column(Name = "id", IsPrimaryKey = true, CanBeNull = false)]
        private long? id = null;
        public long ID => id ?? 0;

        [Column(Name = "path", CanBeNull = false)]
        private string path = "";
        public string Path => path;

        [Column(Name = "date", CanBeNull = false)]
        private long date = 0;
        public DateTime Date {
            get => DBBase.AsTime(date);
            private set => setProp(callerName(), ref date, value.ToFileTimeUtc());
        }

        [Column(Name = "size", CanBeNull = false)]
        private long size = 0;
        public long Size {
            get => size;
            private set => setProp(callerName(), ref size, value);
        }

        [Column(Name = "mark", CanBeNull = true)]
        private string mark = "";
        public string Mark {
            get => mark;
            set => setProp(callerName(), ref mark, value);
        }

        [Column(Name = "checked", CanBeNull = false)]
        private int _checked = 0;
        public bool Checked {
            get => _checked != 0;
            set => setProp(callerName(), ref _checked, value ? 1 : 0);
        }

        [Column(Name = "flag", CanBeNull = true)]
        private int flag = 0;
        public int Flag {
            get => flag;
            set => setProp(callerName(), ref flag, value);
        }

        [Column(Name = "rating", CanBeNull = false)]
        private int rating = (int)Rating.NORMAL;
        public Rating Rating {
            get => (Rating)rating;
            set => setProp(callerName(), ref rating, (int)value);
        }

        [Column(Name = "lastPlay", CanBeNull = true)]
        private long lastPlay = 0;
        public DateTime LastPlayDate {
            get => DBBase.AsTime(lastPlay);
            set => setProp(callerName(), ref lastPlay, value.ToFileTimeUtc());
        }

        [Column(Name = "playCount", CanBeNull = true)]
        private long playCount = 0;
        public long PlayCount {
            get => playCount;
            set => setProp(callerName(), ref playCount, value);
        }
        [Column(Name = "trim_start", CanBeNull = true)]
        private ulong trimStart = 0;
        public ulong TrimStart {
            get => trimStart;
            set => setProp(callerName(), ref trimStart, value, "TrimRange");
        }
        [Column(Name = "trim_end", CanBeNull = true)]
        private ulong trimEnd = 0;
        public ulong TrimEnd {
            get => trimEnd;
            set => setProp(callerName(), ref trimEnd, value, "TrimRange");
        }

        [Column(Name = "title", CanBeNull = true)]
        private string title = "";
        public string Title {
            get => title ?? "";
            set => setProp(callerName(), ref title, value);
        }

        [Column(Name = "desc", CanBeNull = true)]
        private string desc = "";
        public string Desc {
            get => desc;
            set => setProp(callerName(), ref desc, value);
        }
        [Column(Name = "aspect", CanBeNull = true)]
        private int aspect = (int)Aspect.AUTO;
        public Aspect Aspect {
            get => (Aspect)aspect;
            set => setProp(callerName(), ref aspect, (int)value);
        }

        [Column(Name = "duration", CanBeNull = true)]
        private ulong duration = 0;
        public ulong Duration {
            get => duration;
            set => setProp(callerName(), ref duration, value);
        }

        static Regex regId = new Regex(@"(?<id>\d{6}-\d{3})|(?<id>\w+)_[a-z]{3}_[a-z]");
        static Regex regId2 = new Regex(@"sample(?:_low)*-(?<id>\d+)");

        private string getKeyFromName(string name) {
            var m = regId.Match(name);
            if (m.Success) {
                var key = m.Groups["id"]?.Value;
                if (!string.IsNullOrEmpty(key)) return key;
            }
            m = regId2.Match(name);
            if (m.Success) {
                var key = m.Groups["id"]?.Value;
                if (!string.IsNullOrEmpty(key)) return $"({key})";
            }
            return name;
        }

        public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

        public string Type => System.IO.Path.GetExtension(Path);

        public string KeyFromName => getKeyFromName(Name);

        public string TrimRange => $"{TrimStart / 1000}-{TrimEnd / 1000}";

        public bool HasFile => PathUtil.isFile(Path);

        public bool Delete() {
            if (PathUtil.isFile(Path)) {
                if (!PathUtil.safeDeleteFile(Path)) {
                    return false;
                }
            }
            Flag = 1;
            return true;
        }

        public static PlayItem Create(string path, DateTime date, long size, ulong duration) {
            var r = new PlayItem();
            r.path = path;
            r.Date = date;
            r.size = size;
            r.Duration = duration;
            return r;
        }
        public static PlayItem Create(string path) {
            return new PlayItem() { path = path };
        }
        public static PlayItem Create(WfItem s) {
            var r = Create(s.Path, s.Date, s.Size, 0);
            r.Aspect = s.Aspect;
            r.LastPlayDate = s.LastPlayDate;
            r.PlayCount = s.PlayCount;
            r.Mark = s.Mark;
            r.Rating = s.Rating;
            r.TrimStart = s.TrimStart;
            r.TrimEnd = s.TrimEnd;
            return r;
        }

        public PlayItem ApplyDxx(DxxStorage dxx) {
            if(dxx!=null) {
                var x = dxx.DxxTable.List.ToList().Where((c) => c.Path == Path).FirstOrDefault();
                //DxxItem x = null;
                //foreach(var v in dxx.DxxTable.List) {
                //    if(v.Path == Path) {
                //        x = v;
                //    }
                //}


                //var y = dxx.DxxTable.List.Where((c) => c.Path == Path);
                //var x = y.FirstOrDefault();
                if (x!=null) {
                    this.Title = x.Desc;
                    LoggerEx.debug(x.Desc);
                }
            }
            return this;
        }
        public PlayItem ComplementDuration() {
            if(Duration==0) {
                var d = MediaInfo.GetDuration(Path);
                if (d.HasValue) {
                    Duration = (ulong)d.Value.TotalMilliseconds;
                    if(TrimEnd>0) {
                        TrimEnd = Duration - TrimEnd;
                    }
                } else {
                    TrimEnd = 0;
                }
            }
            return this;
        }

        public PlayItem ComplementAll() {
            var fi = new FileInfo(path);
            Date = fi.CreationTimeUtc;
            size = fi.Length;
            ComplementDuration();
            return this;
        }

        public class DeleteItemCommandImpl : SimpleCommand {
            public DeleteItemCommandImpl(PlayItem item) : base(() => {
                if(item.Delete()) {
                    App.MainWindow.UpdateList();
                }
            }) {}
        }
        public DeleteItemCommandImpl DeleteItemCommand => new DeleteItemCommandImpl(this);

        public class ResetCounterCommandImpl:SimpleCommand {
            private PlayItem playItem;
            public ResetCounterCommandImpl(PlayItem item) : base(() => {
                item.PlayCount = 0;
            }) { playItem = item; }
            public override bool Enabled {
                get => playItem.PlayCount > 0;
                set { }
            }
        }
        public ResetCounterCommandImpl ResetCounterCommand => new ResetCounterCommandImpl(this);

        public class CopyItemPathCommandImpl:SimpleCommand {
            public CopyItemPathCommandImpl(PlayItem item) : base(() => {
                var anal = FFAnalyzer.Analyze(item.Path).ToString();
                Debug.WriteLine(anal);
                Clipboard.SetText(item.Path);
            }) { }
        }
        public CopyItemPathCommandImpl CopyItemPathCommand => new CopyItemPathCommandImpl(this);

        static private string TempPathFrom(string path, string suffix) {
            var dir = System.IO.Path.GetDirectoryName(path);
            var ext = System.IO.Path.GetExtension(path);
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            return System.IO.Path.Combine(dir, $"{name}{suffix}{ext}");
        }

        private double calcBitRateFactor(FFAnalyzer.Analysis info) {
            var video = info.Video;
            return video.BitRate / (video.Width * video.Height * video.FrameRate / 30);
        }

        public async Task<bool> Compress(IFFProgress progress, bool forceCompress=false) {
            var inputInfo = FFAnalyzer.Analyze(Path);
            if (inputInfo.Video == null) return false;


            //var video = inputInfo.Video;
            //var w = video.Width;
            //var h = video.Height;
            //var size = inputInfo.Size;
            //var duration = inputInfo.Duration;
            //var bitrate = video.BitRate;
            //var framerate = video.FrameRate / 30.0;
            //var s = (long)w * (long)h;
            //var da = s * duration.TotalSeconds * framerate;
            //var eff = (double)da / (double)size;    // １バイト当たりの情報量
            //var brf = (double)bitrate / ((double)s*framerate);         // １ピクセルあたりのビットレート
            //var rbr = 2.0 * s * framerate;

            //Debug.WriteLine($"{Name}: {w}x{h} bitrate={bitrate} (rbr={rbr}: efficient={eff} / bitrate-factor={brf}");
            //return true;



            var chapterEditor = new ChapterEditor();
            var trimmed = false;
            ConvertResult result = null;
            var outPath = TempPathFrom(this.Path, "_comp");
            chapterEditor.OnMediaOpened(this);
            var disabled = chapterEditor.DisabledRanges.Value;
            if (disabled!=null && disabled.Count>0) {
                // トリミングが必要
                Debug.WriteLine($"Trimming:{ID} {Name}");
                trimmed = true;
                var enabledRange = chapterEditor.GetEnabledRanges().ToList();
                result = await FFApi.TrimmingAsync(this.Path, outPath, enabledRange, FFApi.ExtractOption.ACCURATE_COMPRESS, progress, inputInfo);
            } else {
                if(!forceCompress && Math.Max(inputInfo.Video.Width,inputInfo.Video.Height)<=FFApi.MAX_LENGTH && inputInfo.Video.FrameRate<(double)FFApi.MAX_FPS) {
                    // 一定以下の画像サイズなら処理しない
                    Debug.WriteLine($"Skipped:{ID} {Name}");
                    Debug.WriteLine(inputInfo.ToString());
                    return true;    // Compress不要
                }
                Debug.WriteLine($"Compressing:{ID} {Name}");
                result = await FFApi.CompressAsync(this.Path, outPath, progress, inputInfo);
            }
            if (!result.Result) return false;
            Debug.WriteLine(result.ToString());
            Debug.WriteLine($"BitRateFactor: {calcBitRateFactor(result.Before)} --> {calcBitRateFactor(result.After)}");

            File.Delete(this.Path);
            File.Move(outPath, this.Path);
            this.Size = result.After.Size;
            this.Duration = result.After.DurationMs;

            if (trimmed) {
                // トリミングされたら、チャプター情報 と Triming情報をクリア
                App.Instance.DB.ChapterTable.DeleteChaptersOfOwner(this.ID);
                this.TrimStart = 0;
                this.TrimEnd = 0;
            }
            //App.MainWindow.UpdateList();
            return true;
        }

        public class CompressItemCommandImpl:SimpleCommand {
            public CompressItemCommandImpl(PlayItem item) : base(() => {
                App.MainWindow.CompressItems();


                //var srcPath = item.Path;
                //var chapterEditor = new ChapterEditor();
                //var trimmed = false;
                //FFAnalyzer.Analysis originalInfo = null;
                //chapterEditor.OnMediaOpened(item);
                //if(chapterEditor.DisabledRanges.Value?.FirstOrDefault()!=null) {
                //    // トリミングが必要
                //    var enabledRange = chapterEditor.GetEnabledRanges().ToList();
                //    var trimFile = TempPathFrom(item.Path,"_trim");
                //    var trim = FFApi.Trimming(item.Path, trimFile, enabledRange, null, null);
                //    if(trim.Result) {
                //        trimmed = true;
                //        srcPath = trimFile;
                //        originalInfo = trim.Before;
                //    } else {
                //        return;
                //    }
                //}

                //var outPath = TempPathFrom(item.Path, "_comp");
                //var comp = FFApi.Compress(srcPath, outPath,null);
                //var result = new FFApi.ConvertResult(comp.Result, originalInfo ?? comp.Before, comp.After, comp.Exception);
                //Debug.WriteLine(result.ToString());
                //if(comp.Result && comp.After.Size<comp.Before.Size) {
                //    File.Delete(item.Path);
                //    File.Move(outPath, item.Path);
                //    item.Size = comp.After.Size;
                //    item.Duration = (ulong)comp.After.Video.Duration.TotalMilliseconds;
                //} else if(trimmed) {
                //    File.Delete(srcPath);
                //    File.Move(srcPath, item.Path);
                //    item.Size = comp.Before.Size;
                //    item.Duration = (ulong)comp.Before.Video.Duration.TotalMilliseconds;
                //} else {
                //    return;
                //}
                //if (trimmed) {
                //    // トリミングされたら、チャプター情報 と Triming情報をクリア
                //    App.Instance.DB.ChapterTable.DeleteChaptersOfOwner(item.ID);
                //    item.TrimStart = 0;
                //    item.TrimEnd = 0;
                //}
                //App.MainWindow.UpdateList();
            }) { }
        }
        public CompressItemCommandImpl CompressItemCommand => new CompressItemCommandImpl(this);
    }

    public class PlayListTable : StorageTable<PlayItem> {
        public PlayListTable(SQLiteConnection connection)
            : base(connection, useAutoIncrement:true) {
        }
        public override bool Contains(PlayItem item) {
            return List.Where((c) => c.Path == item.Path).Any();
        }
        public PlayItem GetByPath(string path) {
            return List.Where((c) => c.Path == path).SingleOrDefault();
        }
    }

    class PlayItemComparator : IEqualityComparer<PlayItem> {
        public bool Equals(PlayItem x, PlayItem y) {
            return x.Path == y.Path;
        }

        public int GetHashCode(PlayItem obj) {
            return obj.Path.GetHashCode();
        }
    }
}


