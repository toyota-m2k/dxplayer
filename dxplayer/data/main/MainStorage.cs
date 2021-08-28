using dxplayer.data.dxx;
using dxplayer.settings;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace dxplayer.data.main {
    public class MainStorage : DBBase {
        private const string APP_NAME = "DxPlayer";
        private const int DB_VERSION = 1;

        protected override void checkExistingDB() {
            // 新規作成でなければ内容をチェック
            var appName = getAppName();
            if (appName != APP_NAME) {
                throw new FormatException($"DB file is not for {APP_NAME}");
            }
            var version = getVersion();
            if (version > DB_VERSION) {
                throw new FormatException($"Newer DB version. ({version})");
            }
        }

        protected override void initTables(bool created) {
            executeSql(
                @"CREATE TABLE IF NOT EXISTS t_play_list (
                    id INTEGER PRIMARY KEY,
                    path TEXT NOT NULL UNIQUE,
                    date INTEGER NOT NULL,
                    size INTEGER NOT NULL,
                    checked INTEGER NOT NULL,
                    rating INTEGER NOT NULL,
                    lastPlay INTEGER NOT NULL,
                    playCount INTEGER NOT NULL,
                    trim_start INTEGER DEFAULT '0',
                    trim_end INTEGER DEFAULT '0',
                    title TEXT,
                    desc TEXT,
                    aspect INTEGER NOT NULL DEFAULT '0',
                    duration INTEGER NOT NULL,
                    mark TEXT,
                    flag INTEGER DEFAULT '0'
                )",
                @"CREATE INDEX IF NOT EXISTS idx_category ON t_play_list(date)",
                @"CREATE INDEX IF NOT EXISTS idx_category ON t_play_list(lastPlay)",
                @"CREATE INDEX IF NOT EXISTS idx_category ON t_play_list(name)",

                @"CREATE TABLE IF NOT EXISTS t_map (
                    name TEXT NOT NULL PRIMARY KEY,
                    ivalue INTEGER DEFAULT '0',
                    svalue TEXT
                )",
                @"CREATE TABLE IF NOT EXISTS t_chapter (
	                id	INTEGER NOT NULL PRIMARY KEY,
                    owner INTEGER NOT NULL,
                    position  INTEGER NOT NULL,
                    label TEXT,
                    skip INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY(owner) REFERENCES t_play_list(id),
                    UNIQUE(owner,position)
                )",
                @"CREATE TABLE IF NOT EXISTS t_target_folders (
                    path TEXT NOT NULL PRIMARY KEY
                )",
                ""
            );

            if (created) {
                // 新規DB
                setAppName();
                setVersion(DB_VERSION);
            }
        }

        public PlayListTable PlayListTable { get; }
        public TargetFolderTable TargetFolderTable { get; }
        public ChapterTable ChapterTable { get; }

        private MainStorage(string path, bool ro = false) : base(path, ro) {
            PlayListTable = new PlayListTable(Connection);
            TargetFolderTable = new TargetFolderTable(Connection);
            ChapterTable = new ChapterTable(Connection);
        }

        public static bool CheckDB(string path) {
            try {
                using (new MainStorage(path, ro: true)) {
                    return true;
                }
            }
            catch (Exception) {
                return false;
            }
        }

        public static MainStorage OpenDB(string path) {
            try {
                return new MainStorage(path);
            }
            catch (Exception e) {
                LoggerEx.error(e);
                return null;
            }
        }

        public int getVersion() {
            try {
                using (var cmd = Connection.CreateCommand()) {
                    cmd.CommandText = $"SELECT ivalue FROM t_map WHERE name='version'";
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.Read()) {
                            return Convert.ToInt32(reader["ivalue"]);
                        }
                    }
                }
                return 0;
            }
            catch (Exception) {
                return 0;
            }
        }

        public bool setVersion(int version) {
            using (var cmd = Connection.CreateCommand()) {
                try {
                    cmd.CommandText = $"UPDATE t_map SET ivalue='{version}' WHERE name='version'";
                    if (1 == cmd.ExecuteNonQuery()) {
                        return true;
                    }
                    cmd.CommandText = $"INSERT INTO t_map (name,ivalue) VALUES('version','{version}')";
                    return 1 == cmd.ExecuteNonQuery();
                }
                catch (SQLiteException) {
                    return false;
                }
            }
        }

        public string getAppName() {
            try {
                using (var cmd = Connection.CreateCommand()) {
                    cmd.CommandText = $"SELECT svalue FROM t_map WHERE name='appName'";
                    using (var reader = cmd.ExecuteReader()) {
                        if (reader.Read()) {
                            return Convert.ToString(reader["svalue"]);
                        }
                    }
                }
                return null;
            }
            catch (Exception) {
                return null;
            }
        }

        public bool setAppName() {
            using (var cmd = Connection.CreateCommand()) {
                try {
                    cmd.CommandText = $"UPDATE t_map SET svalue='{APP_NAME}' WHERE name='appName'";
                    if (1 == cmd.ExecuteNonQuery()) {
                        return true;
                    }
                    cmd.CommandText = $"INSERT INTO t_map (name,svalue) VALUES('appName','{APP_NAME}')";
                    return 1 == cmd.ExecuteNonQuery();
                }
                catch (SQLiteException) {
                    return false;
                }
            }
        }

        public override void Dispose() {
            PlayListTable.Dispose();
            base.Dispose();
        }

        /**
         * 指定されたフォルダ内の新しいファイル（DB未登録なファイル）をDB追加する。
         */
        private void InsertAddedFiles(string folderPath, DxxStorage dxdb, IStatusBar statusBar, string prefix) {
            var comparator = new PlayItemComparator();
            var videoExt = new[] { ".mp4", ".wmv", ".avi", ".mov", ".avi", ".mpg", ".mpeg", ".mpe", ".ram", ".rm" };
            var items = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                           .Where(path => videoExt.Any(x => path.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                           .Select(path => PlayItem.Create(path))
                           .Except(PlayListTable.List, comparator)
                           .ToList();
            int count = 0, totalCount = items.Count();
            foreach (var item in items) {
                count++;
                statusBar.OutputStatusMessage($"{prefix} ({count}/{totalCount}) : {item.Name}");
                PlayListTable.Insert(item.ComplementAll().ApplyDxx(dxdb), false);
            }
            PlayListTable.Update();
        }

        /**
         * DBに登録されているが実ファイルが消えているものがあればDBから削除する。
         */
        private void DeleteRemovedFiles(IStatusBar statusBar, string prefix) {
            int count = 0;
            var items = PlayListTable.List.Where((c) => {
                statusBar.OutputStatusMessage($"Checking ({++count}) : {c.Name}");
                return !PathUtil.isFile(c.Path);
            }).ToList();
            statusBar.OutputStatusMessage($"Deleting {items.Count} items");
            PlayListTable.DeleteAll(items);
        }

        public async Task AddTargetFolder(string folderPath, IStatusBar statusBar) {
            if (string.IsNullOrEmpty(folderPath)) return;

            statusBar.OutputStatusMessage($"Importing from {folderPath} ...");
            await Task.Run(() => {
                if (!TargetFolderTable.Contains(folderPath)) {
                    TargetFolderTable.Insert(TargetFolders.Create(folderPath));
                    TargetFolderTable.Update();
                }
                using (var dxdb = DxxStorage.SafeOpen(Settings.Instance.DxxDBPath)) {
                    InsertAddedFiles(folderPath, dxdb, statusBar, "Importing");
                }
                statusBar.FlashStatusMessage($"import: completed.");
            });
        }

        public async Task RefreshDB(IStatusBar statusBar) {
            statusBar.OutputStatusMessage($"Refresh DB: ");
            await Task.Run(() => {
                using (var dxdb = DxxStorage.SafeOpen(Settings.Instance.DxxDBPath)) {
                    foreach (var tf in TargetFolderTable.List) {
                        InsertAddedFiles(tf.Path, dxdb, statusBar, "Appending");
                    }
                }
                DeleteRemovedFiles(statusBar, "Deleting");
                PlayListTable.Update();
                statusBar.FlashStatusMessage($"Refresh: completed.");
            });
        }

    }
}
