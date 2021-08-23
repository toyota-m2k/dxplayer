using System.Data.Linq.Mapping;

namespace dxplayer.data.wf
{
    [Table(Name = "t_target_folders")]
    public class WfTargetFolders {
        [Column(Name = "path", IsPrimaryKey = true, CanBeNull = false)]
        private string path = "";
        public string Path => path;
    }
}
