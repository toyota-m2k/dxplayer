using dxplayer.data.main;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;

namespace dxplayer.data {
    [Table(Name = "t_chapter")]
    public class ChapterEntry {
        [Column(Name = "id", IsPrimaryKey = true)]
        private long? id { get; set; } = null;

        [Column(Name = "owner", CanBeNull = false)]
        public long Owner { get; private set; }

        [Column(Name = "position", CanBeNull = false)]
        public ulong Position{ get; private set; }

        [Column(Name = "label", CanBeNull = true)]
        public string Label { get; set; }

        [Column(Name = "skip", CanBeNull = false)]
        private int skip { get; set; }
        public bool Skip {
            get => skip!=0;
            set { skip = value ? 1 : 0; }
        }

        public ChapterEntry() {
            Owner = 0;
            Position = 0;
            Label = null;
            skip = 0;
        }

        static public ChapterEntry Create(long owner, ulong pos, bool skip=false, string label=null) {
            return new ChapterEntry() { Owner = owner, Position = pos, Skip = skip, Label = label };
        }
        static public ChapterEntry Create(long owner, ChapterInfo info) {
            return new ChapterEntry() { Owner = owner, Position = info.Position, Skip = info.Skip, Label = info.Label };
        }

        public ChapterInfo ToChapterInfo(ChapterEditor editor) {
            return new ChapterInfo(editor, Position, Skip, Label);
        }
    }

    public class ChapterTable : StorageTable<ChapterEntry> {
        public ChapterTable(SQLiteConnection connection) : base(connection, true) { }
        public override bool Contains(ChapterEntry entry) {
            return Table.Where((c) => c.Owner == entry.Owner && c.Position == entry.Position).Any();
        }

        class ChapterGroup {
            public string Owner { get; }
            public List<ChapterEntry> Chapters { get; }

            public ChapterGroup(string owner, List<ChapterEntry> list) {
                Owner = owner;
                Chapters = list;
            }
        };

        public IEnumerable<ChapterEntry> GetChapterEntries(long owner) {
            return Table.Where((c) => c.Owner == owner);
        }

        public ChapterList GetChapterList(ChapterEditor editor, long owner) {
            return new ChapterList(owner, Table.Where((c) => c.Owner == owner).Select((c)=>c.ToChapterInfo(editor)));
        }

        private class PositionComparator : IEqualityComparer<ChapterInfo> {
            public bool Equals(ChapterInfo x, ChapterInfo y) {
                return x.Position == y.Position;
            }

            public int GetHashCode(ChapterInfo obj) {
                return obj.Position.GetHashCode();
            }
        }

        private static PositionComparator PComp = new PositionComparator();

        // Autoincrementのprimary keyのせいで、DuplicateKeyExceptionが出る問題対策
        public void AddAll(IEnumerable<ChapterEntry> source) {
            foreach (var a in source) {
                Table.InsertOnSubmit(a);
                FlashForce();
            }
        }


        public void UpdateByChapterList(ChapterEditor editor, ChapterList updated) {
            var current = GetChapterList(editor, updated.Owner);

            var appended = updated.Values.Except(current.Values, PComp).Select((c)=>ChapterEntry.Create(updated.Owner, c)).ToList();
            var deleted = current.Values.Except(updated.Values, PComp).Select((c) => ChapterEntry.Create(updated.Owner, c)).ToList();
            var modified = updated.Values.Where((c)=>c.IsModified).Intersect(current.Values, PComp);

            foreach(var m in modified) {
                var entry = Table.Where((c) => c.Position == m.Position && c.Owner == current.Owner).SingleOrDefault();
                entry.Skip = m.Skip;
                entry.Label = m.Label;
            }

            foreach(var a in appended) {
                Table.InsertOnSubmit(a);
                FlashForce();
            }
            // 残念ながら、Autoincrementのprimary keyのせいで、DuplicateKeyExceptionが出るから、InsertAllOnSubmitは使えない。
            //Table.InsertAllOnSubmit(appended);
            // Table.DeleteAllOnSubmit(deleted);
            foreach (var d in deleted) {
                var entry = Table.Where((c) => c.Position == d.Position && c.Owner == current.Owner).SingleOrDefault();
                Table.DeleteOnSubmit(entry);
            }

            Update();
            updated.ResetModifiedFlag();
        }

        public void DeleteChaptersOfOwner(long owner) {
            var entries = Table.Where((c) => c.Owner == owner);
            foreach (var e in entries) {
                Table.DeleteOnSubmit(e);
            }
            Update();
        }
    }
}
