using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;

namespace dxplayer.data.main
{
    [Table(Name = "t_target_folders")]
    public class TargetFolders {
        [Column(Name = "path", IsPrimaryKey = true, CanBeNull = false)]
        private string path = "";
        public string Path => path;

        public static TargetFolders Create(string path_) {
            return new TargetFolders() { path = path_ };
        }
    }


    public class TargetFolderTable : StorageTable<TargetFolders> {
        public TargetFolderTable(SQLiteConnection connection)
            : base(connection, false) {
        }
        public override bool Contains(TargetFolders item) {
            return Contains(item.Path);
        }
        public bool Contains(string path) {
            return List.Where((c) => c.Path == path).Any();
        }
    }
}
