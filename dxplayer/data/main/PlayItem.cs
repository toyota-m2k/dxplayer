﻿using dxplayer.data.dxx;
using dxplayer.data.wf;
using dxplayer.misc;
using dxplayer.player;
using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace dxplayer.data.main {
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
        [Column(Name = "path", IsPrimaryKey = true, CanBeNull = false)]
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
        public long Size => size;

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

        public void Delete() {
            PathUtil.safeDeleteFile(Path);
        }

        public static PlayItem Create(string path, DateTime date, long size, ulong duration) {
            var r = new PlayItem();
            r.path = path;
            r.Date = date;
            r.size = size;
            r.Duration = duration;
            return r;
        }
        public static PlayItem Create(WfItem s) {
            if(!PathUtil.isFile(s.Path)) {
                return null;
            }
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
    }

    public class PlayListTable : StorageTable<PlayItem> {
        public PlayListTable(SQLiteConnection connection)
            : base(connection, false) {
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


