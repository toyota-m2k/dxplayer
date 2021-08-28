using io.github.toyota32k.toolkit.utils;
using System;
using System.Data.SQLite;

namespace dxplayer.data
{
    public abstract class DBBase : IDisposable {
        static LoggerEx logger = new LoggerEx("DATA");

        public bool ReadOnly { get; private set; }
        protected SQLiteConnection Connection { get; private set; }
        public string DBPath { get; private set; }

        /**
         * 開いたDBファイルがこのアプリのDBかをチェックする。
         * 不適合な場合はExceptionをスローすること。
         * 新規の場合は呼ばない。
         */
        protected abstract void checkExistingDB();
        /**
         * テーブルを初期化。
         * ReadOnlyの場合は呼ばない。
         * @param created: true:新規 / false:既存
         */
        protected abstract void initTables(bool created);

        public DBBase(string path, bool ro) {
            ReadOnly = ro;
            DBPath = path;

            bool onMemory = path == ":memory";
            bool exists = onMemory || PathUtil.isExists(path);
            if (!exists&&ro) {
                throw new System.IO.FileNotFoundException("No DB File", path);
            }
            bool creation = false;
            if (onMemory || !exists) {
                // onMemoryDBまたは、存在しないときは新規作成
                creation = true;
            }
            try {
                var builder = new SQLiteConnectionStringBuilder() { DataSource = path, ReadOnly=ro };
                Connection = new SQLiteConnection(builder.ConnectionString);
                Connection.Open();
                if(!creation) {
                    checkExistingDB();
                }
                if (!ro) {
                    initTables(creation);
                }
            }
            catch (Exception ex) {
                Dispose();
                logger.error(ex);
                throw ex;
            }
        }

        // DB操作ヘルパー
        protected void executeSql(params string[] sqls) {
            using (var cmd = Connection.CreateCommand()) {
                foreach (var sql in sqls) {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public virtual void Dispose() {
            if (Connection != null) {
                Connection.Close();
                Connection.Dispose();
                Connection = null;
            }
        }

        private static readonly DateTime EpochDate = new DateTime(1970, 1, 1, 0, 0, 0);
        public static DateTime AsTime(object obj) {
            try {
                if (obj != null && obj != DBNull.Value) {
                    var t = Convert.ToInt64(obj);
                    if (t > 0) {
                        return DateTime.FromFileTimeUtc(Convert.ToInt64(obj));
                    }
                }
            }
            catch (Exception e) {
                Logger.error(e);
            }
            return EpochDate;
        }
    }
}
