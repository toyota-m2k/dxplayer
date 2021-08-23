using System;
using System.Data.Linq.Mapping;

namespace dxplayer.data.dxx
{
    [Table(Name = "t_storage")]
    public class DxxItem {
        [Column(Name = "id", IsPrimaryKey = true)]
        public long? id { get; private set; } = null;

        [Column(Name = "url", CanBeNull = false)]
        private string url = "";
        public string Url => url;

        [Column(Name = "name", CanBeNull = false)]
        private string name = "";
        public string Name => name;

        [Column(Name = "path", CanBeNull = false)]
        private string path = "";
        public string Path => path;

        [Column(Name = "status", CanBeNull = false)]
        private int status = 0;
        public int Status => status;

        [Column(Name = "desc", CanBeNull = true)]
        private string desc = "";
        public string Desc => desc;

        [Column(Name = "driver", CanBeNull = true)]
        private string driver = "";
        public string Driver => driver;

        [Column(Name = "flags", CanBeNull = true)]
        private int? flags = 0;
        public int Flags => flags??0;

        [Column(Name = "date", CanBeNull = true)]
        private long? date = 0;
        public DateTime Date {
            get => DBBase.AsTime(date??0);
        }
    }
}
