using dxplayer.data.main;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dxplayer.data.wf {
    [Table(Name = "t_playlist")]
    public class WfItem {
        [Column(Name = "id", IsPrimaryKey = true)]
        public long? id { get; private set; } = null;

        [Column(Name = "path", CanBeNull = false)]
        private string path = "";
        public string Path => path;

        [Column(Name = "date", CanBeNull = false)]
        private long date = 0;
        public DateTime Date {
            get => DBBase.AsTime(date);
        }
        [Column(Name = "size", CanBeNull = false)]
        private long size = 0;
        public long Size => size;

        [Column(Name = "mark", CanBeNull = true)]
        private string mark = "";
        public string Mark => mark;

        [Column(Name = "rating", CanBeNull = false)]
        private int rating = (int)WfRatings.NORMAL;
        public Rating Rating => fromWfRating((WfRatings)rating);

        [Column(Name = "lastPlay", CanBeNull = true)]
        private long lastPlay = 0;
        public DateTime LastPlayDate => DBBase.AsTime(lastPlay);

        [Column(Name = "playCount", CanBeNull = true)]
        private long playCount = 0;
        public long PlayCount {
            get => playCount;
        }
        [Column(Name = "trim_start", CanBeNull = true)]
        private long trimStart = 0;
        public ulong TrimStart => (ulong)trimStart;

        [Column(Name = "trim_end", CanBeNull = true)]
        private long trimEnd = 0;
        public ulong TrimEnd => (ulong)trimEnd;

        [Column(Name = "aspect", CanBeNull = true)]
        private int aspect = (int)Aspect.AUTO;
        public Aspect Aspect => (Aspect)aspect;

        private enum WfRatings {
            GOOD = 0,       // 優良
            NORMAL,         // ふつう
            BAD,           // 一覧に表示しても再生はしない
            DREADFUL,       // 削除予定
        }

        Rating fromWfRating(WfRatings rating) {
            switch(rating) {
                case WfRatings.GOOD: return Rating.GOOD;
                case WfRatings.NORMAL: return Rating.NORMAL;
                case WfRatings.BAD: return Rating.BAD;
                case WfRatings.DREADFUL:    return Rating.DREADFUL;
                default: return Rating.NORMAL;
            }
        }
    }
}
