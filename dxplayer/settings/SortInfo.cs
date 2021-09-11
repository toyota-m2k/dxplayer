using dxplayer.data.main;
using io.github.toyota32k.toolkit.view;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dxplayer.settings {
    public class SortInfo : PropertyChangeNotifier/*, IComparer<PlayItem>*/ {
        public enum SortKey {
            ID,
            NAME,
            TYPE,
            SIZE,
            DURATION,
            DATE,
            LAST_PLAY,
            COUNT,
            TRIMMING,
            CHECKED,
            RATING,
            ASPECT,
            TITLE,
            PATH,
        }
        public enum SortOrder {
            ASCENDING = 1,
            DESCENDING = -1,
        }


        public event Action SortUpdated;
        private SortKey mPrimaryKey = SortKey.DATE;
        private SortKey mSecondaryKey = SortKey.PATH;
        private SortOrder mOrder = SortOrder.ASCENDING;
        private bool mShuffle = false;

        public SortKey PrimaryKey { 
            get => mPrimaryKey;
            set { mPrimaryKey = value; SortUpdated?.Invoke(); }
        }
        public SortKey SecondaryKey { 
            get => mSecondaryKey;
            set { mSecondaryKey = value; SortUpdated?.Invoke(); }
        }
        public SortOrder Order { 
            get => mOrder;
            set { mOrder = value; SortUpdated?.Invoke(); } 
        }
        public bool Shuffle {
            get => mShuffle;
            set => SetShuffle(value, needsUpdateSort:true);
        }
        private void SetShuffle(bool value, bool needsUpdateSort) {
            if (setProp("Shuffle", ref mShuffle, value) && needsUpdateSort) {
                SortUpdated?.Invoke();
            }
        }

        class StringComparer : IComparer<string> {
            private int asc = 1;
            public int Compare(string x, string y) {
                return string.Compare(x, y)*asc;
            }
            public static StringComparer Instance(SortOrder order) => new StringComparer() { asc = (int)order };
        }
        class ULongComparer : IComparer<ulong> {
            private int asc = 1;
            public int Compare(ulong x, ulong y) {
                if (x < y) return -asc;
                else if (x > y) return asc;
                else return 0;
            }
            public static ULongComparer Instance(SortOrder order) => new ULongComparer() { asc = (int)order };
        }
        class LongComparer : IComparer<long> {
            private int asc = 1;
            public int Compare(long x, long y) {
                if (x < y) return -asc;
                else if (x > y) return asc;
                else return 0;
            }
            public static LongComparer Instance(SortOrder order) => new LongComparer() { asc = (int)order };
        }
        class IntComparer : IComparer<int> {
            private int asc = 1;
            public int Compare(int x, int y) {
                if (x < y) return -asc;
                else if (x > y) return asc;
                else return 0;
            }
            public static IntComparer Instance(SortOrder order) => new IntComparer() { asc = (int)order };
        }
        class BoolComparer : IComparer<bool> {
            private int asc = 1;
            public int Compare(bool x, bool y) {
                if (!x && y) return -asc;
                else if (x && !y) return asc;
                else return 0;
            }
            public static BoolComparer Instance(SortOrder order) => new BoolComparer() { asc = (int)order };
        }
        class DateTimeComparer : IComparer<DateTime> {
            private int asc = 1;
            public int Compare(DateTime x, DateTime y) {
                return DateTime.Compare(x, y) * asc;
            }
            public static DateTimeComparer Instance(SortOrder order) => new DateTimeComparer() { asc = (int)order };
        }


        private IOrderedEnumerable<PlayItem> OrderByPrimaryKey(IEnumerable<PlayItem> list) {
            switch (PrimaryKey) {
                case SortKey.ID:
                    return list.OrderBy(e => e.KeyFromName, StringComparer.Instance(Order));
                case SortKey.NAME:
                    return list.OrderBy(e => e.Name, StringComparer.Instance(Order));
                case SortKey.TYPE:
                    return list.OrderBy(e => e.Type, StringComparer.Instance(Order));
                case SortKey.SIZE:
                    return list.OrderBy(e => e.Size, LongComparer.Instance(Order));
                case SortKey.DURATION:
                    return list.OrderBy(e => e.Duration, ULongComparer.Instance(Order));
                case SortKey.DATE:
                    return list.OrderBy(e => e.Date, DateTimeComparer.Instance(Order));
                case SortKey.LAST_PLAY:
                    return list.OrderBy(e => e.LastPlayDate, DateTimeComparer.Instance(Order));
                case SortKey.COUNT:
                    return list.OrderBy(e => e.PlayCount, LongComparer.Instance(Order));
                case SortKey.TRIMMING:
                    var comp = ULongComparer.Instance(Order);
                    return list.OrderBy(e => e.TrimStart, comp).ThenBy(e => e.TrimEnd, comp);
                case SortKey.CHECKED:
                    return list.OrderBy(e => e.Checked, BoolComparer.Instance(Order));
                case SortKey.RATING:
                    return list.OrderBy(e => (int)e.Rating, IntComparer.Instance(Order));
                case SortKey.ASPECT:
                    return list.OrderBy(e => (int)e.Aspect, IntComparer.Instance(Order));
                case SortKey.TITLE:
                    return list.OrderBy(e => e.Title, StringComparer.Instance(Order));
                case SortKey.PATH:
                default:
                    return list.OrderBy(e => e.Path, StringComparer.Instance(Order));
            }
        }
        private IOrderedEnumerable<PlayItem> OrderBySecondaryKey(IOrderedEnumerable<PlayItem> list) {
            SortKey key = SecondaryKey;
            if(PrimaryKey==key) {
                if(PrimaryKey==SortKey.DATE||PrimaryKey==SortKey.PATH) {
                    return list;
                }
                key = SortKey.DATE;
            }
            switch (key) {
                case SortKey.ID:
                    return list.ThenBy(e => e.KeyFromName, StringComparer.Instance(Order));
                case SortKey.NAME:
                    return list.ThenBy(e => e.Name, StringComparer.Instance(Order));
                case SortKey.TYPE:
                    return list.ThenBy(e => e.Type, StringComparer.Instance(Order));
                case SortKey.SIZE:
                    return list.ThenBy(e => e.Size, LongComparer.Instance(Order));
                case SortKey.DURATION:
                    return list.ThenBy(e => e.Duration, ULongComparer.Instance(Order));
                case SortKey.DATE:
                    return list.ThenBy(e => e.Date.ToFileTimeUtc(), LongComparer.Instance(Order));
                case SortKey.LAST_PLAY:
                    return list.ThenBy(e => e.LastPlayDate.ToFileTimeUtc(), LongComparer.Instance(Order));
                case SortKey.COUNT:
                    return list.ThenBy(e => e.PlayCount, LongComparer.Instance(Order));
                case SortKey.TRIMMING:
                    var comp = ULongComparer.Instance(Order);
                    return list.ThenBy(e => e.TrimStart, comp).ThenBy(e => e.TrimEnd, comp);
                case SortKey.CHECKED:
                    return list.ThenBy(e => e.Checked, BoolComparer.Instance(Order));
                case SortKey.RATING:
                    return list.ThenBy(e => (int)e.Rating, IntComparer.Instance(Order));
                case SortKey.ASPECT:
                    return list.ThenBy(e => (int)e.Aspect, IntComparer.Instance(Order));
                case SortKey.TITLE:
                    return list.ThenBy(e => e.Title, StringComparer.Instance(Order));
                case SortKey.PATH:
                default:
                    return list.ThenBy(e => e.Path, StringComparer.Instance(Order));
            }
        }

        public IOrderedEnumerable<PlayItem> Sort(IEnumerable<PlayItem> list) {
            return OrderBySecondaryKey(OrderByPrimaryKey(list));

        }

        public void SetSortKey(string v) {
            SetShuffle(false, needsUpdateSort: false);
            var k = SortKeyFromString(v);
            if (k != mPrimaryKey) {
                mSecondaryKey = mPrimaryKey;
                mPrimaryKey = k;
            }
            else {
                mOrder = (SortOrder)((int)mOrder * -1);
            }
            SortUpdated?.Invoke();
        }

        public static SortKey SortKeyFromString(string v) {
            if(string.Compare(v, "Last Play", ignoreCase:true)==0) {
                return SortKey.LAST_PLAY;
            }
            return (SortKey)Enum.Parse(typeof(SortKey), v, true);
        }
    }
}
