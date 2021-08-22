using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dxplayer.data.main {
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
            return List.Where((c) => c.Path == item.Path).Any();
        }
    }
}
