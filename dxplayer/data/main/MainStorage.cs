using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

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
                    path TEXT NOT NULL UNIQUE PRIMARY KEY,
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
                    owner TEXT NOT NULL,
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

        private MainStorage(string path, bool ro=false): base(path, ro) {
            PlayListTable = new PlayListTable(Connection);
            TargetFolderTable = new TargetFolderTable(Connection);
            ChapterTable = new ChapterTable(Connection);
        }

        public static bool CheckDB(string path) {
            try {
                using (new MainStorage(path, ro:true)) {
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
    }
}
