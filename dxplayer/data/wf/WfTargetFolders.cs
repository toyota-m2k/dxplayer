using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace dxplayer.data.wf {
    [Table(Name = "t_target_folders")]
    public class WfTargetFolders {
        [Column(Name = "path", IsPrimaryKey = true, CanBeNull = false)]
        private string path = "";
        public string Path => path;
    }
}
