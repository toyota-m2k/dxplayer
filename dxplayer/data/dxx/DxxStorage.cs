using dxplayer.data.wf;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.data.dxx {
    public class DxxStorage : DBBase {
        public StorageTable<DxxItem> DxxTable { get; }
        public DxxStorage(string path) : base(path, true) {
            DxxTable = new StorageTable<DxxItem>(Connection, true);
        }

        protected override void checkExistingDB() {
            
        }

        protected override void initTables(bool created) {
            throw new NotImplementedException();
        }

        public static DxxStorage SafeOpen(string path) {
            if (string.IsNullOrEmpty(path)) return null;
            try {
                return new DxxStorage(path);
            }
            catch (Exception ex) {
                LoggerEx.error(ex);
                return null;
            }
        }

        internal static IDisposable SafeOpen(object dxxDBPath) {
            throw new NotImplementedException();
        }
    }
}
