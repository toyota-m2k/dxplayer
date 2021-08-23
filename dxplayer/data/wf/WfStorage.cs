using dxplayer.data.dxx;
using dxplayer.data.main;
using dxplayer.data.utils;
using dxplayer.settings;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace dxplayer.data.wf
{
    public class WfStorage : DBBase {
        const string KEY_APP_NAME = "AppName";
        const string KEY_VERSION = "Version";
        const string VAL_APP_NAME = "WfPlayer";
        const int VAL_CURRENT_VERSION = 1;

        public StorageTable<WfItem> PlayListTable { get; }
        public StorageTable<WfTargetFolders> TargetFolderTable { get; }

        public WfStorage(string path) : base(path, true) {
            PlayListTable = new StorageTable<WfItem>(Connection, true);
            TargetFolderTable = new StorageTable<WfTargetFolders>(Connection, true);
        }

        protected override void checkExistingDB() {
            if (getAppName() != VAL_APP_NAME) {
                throw new FormatException($"DB file is not for {VAL_APP_NAME}");
            }
            if (getVersion() != VAL_CURRENT_VERSION) {
                throw new FormatException($"DB version is not match with {VAL_APP_NAME}");
            }
        }

        protected override void initTables(bool created) {
            throw new NotImplementedException();
        }

        private string GetValueAt(string key) {
            using (var cmd = Connection.CreateCommand()) {
                cmd.CommandText = $"SELECT value FROM t_key_value WHERE key='{key}'";
                using (var reader = cmd.ExecuteReader()) {
                    if (reader.Read()) {
                        return Convert.ToString(reader["value"]);
                    }
                }
            }
            return null;
        }


        private string getAppName() {
            try {
                return GetValueAt(KEY_APP_NAME);
            }
            catch (Exception) {
                return null;
            }
        }
        private int getVersion() {
            try {
                return Convert.ToInt32(GetValueAt(KEY_VERSION));
            }
            catch (Exception) {
                return 0;
            }
        }

        public static WfStorage SafeOpen(string path) {
            if (string.IsNullOrEmpty(path)) return null;
            try {
                return new WfStorage(path);
            }
            catch (Exception ex) {
                LoggerEx.error(ex);
                return null;
            }
        }

        public async Task ExportTo(MainStorage dst, IStatusBar statusBar) {
            int count = 0;
            int total = 0;
            int chunkSize = 100;
            var comparator = new PlayItemComparator();
            statusBar.OutputStatusMessage("importing ...");
            await Task.Run(() => {
                using (var dxdb = DxxStorage.SafeOpen(Settings.Instance.DxxDBPath)) {
                    var dstPlayList = dst.PlayListTable;
                    var srcPlayList = this.PlayListTable.List
                                        .Select((w) => PlayItem.Create(w))
                                        .Where((c) => c != null);
                    var appended = srcPlayList
                                        .Except(dstPlayList.List, comparator)
                                        .ToList();
                    total = appended.Count;
                    dstPlayList.WithTransaction(() => {
                        foreach (var newItems in appended.Split(chunkSize)) {
                            count += newItems.Count();
                            dstPlayList.Table.InsertAllOnSubmit(newItems.Select(v => v.ApplyDxx(dxdb).ComplementDuration()));
                            statusBar.OutputStatusMessage($"importing: commiting... {count}/{total}");
                            dstPlayList.Update();
                        }
                        return true;
                    });

#if false
                    var mayBeUpdated = srcPlayList.Intersect(dstPlayList.List, comparator).ToList();
                    total = mayBeUpdated.Count();
                    count = 0;
                    foreach(var src in mayBeUpdated) {
                        count++;
                        var dst = dstPlayList.GetByPath(src.Path);
                        if (dst.LastPlayDate < src.LastPlayDate) {
                            dst.LastPlayDate = src.LastPlayDate;
                            dst.PlayCount = src.PlayCount;
                        }
                        if (count % 100 == 0) {
                            OutputStatusMessage($"updating ... {count}/{total}");
                        }
                    }
#endif

                    statusBar.OutputStatusMessage("importing: folder settings...");
                    var dstFolders = App.Instance.DB.TargetFolderTable;
                    var folders = this.TargetFolderTable.List
                                        .Select((w) => w.Path)
                                        .Except(dstFolders.List.Select((d) => d.Path))
                                        .Select((p) => TargetFolders.Create(p));
                    dstFolders.Table.InsertAllOnSubmit(folders);
                    dstFolders.Update();
                }
            });
            statusBar.FlashStatusMessage("Import completed.");
        }

    }
}
