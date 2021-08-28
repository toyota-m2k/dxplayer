using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SQLite;
using System.Reactive.Subjects;

namespace dxplayer.data {
    public class StorageTable<T> : IDisposable where T : class {
        public DataContext Context;
        public Table<T> Table { get; private set; }
        public Subject<T> AddEvent { get; } = new Subject<T>();
        public Subject<T> DelEvent { get; } = new Subject<T>();
        protected bool UseAutoIncrement { get; }

        public StorageTable(SQLiteConnection connection, bool useAutoIncrement) {
            Context = new DataContext(connection);
            Table = Context.GetTable<T>();
            UseAutoIncrement = useAutoIncrement;
        }

        // SQLite + Linq で、Primary Key を autoincrement にすると、なぜか、連続する複数回のinsertで、DuplicateKeyException が出て失敗する。
        // https://stackoverflow.com/questions/37159299/insert-with-autoincrement
        // では、３つの解決策が示されているが、１，２はダメで、３の、Insertするたびに、Contextを作り直す、という効率の悪そうな方法しかうまく行かなかった。
        public void FlashForce() {
            Update();
            var connection = Context.Connection;
            Context.Dispose();
            Context = new DataContext(connection);
            Table = Context.GetTable<T>();
        }

        //public abstract bool Contains(string key);
        //public abstract bool Contains(T entry);

        public virtual bool Contains(T item) {
            return false;
        }

        public IEnumerable<T> List => Table;

        private void RefreshContext() {
            Update();
            var connection = Context.Connection;
            Context.Dispose();
            Context = new DataContext(connection);
            Table = Context.GetTable<T>();
        }

        public bool Insert(T add, bool update=true) {
            try {
                if(Contains(add)) {
                    return false;
                }
                Table.InsertOnSubmit(add);
                if(UseAutoIncrement) {
                    RefreshContext();
                } else if(update) {
                    Update();
                }
                AddEvent.OnNext(add);
                return true;
            }
            catch (Exception e) {
                Logger.error(e);
                return false;
            }
        }

        public bool InsertAll(IEnumerable<T> adds, bool update=true) {
            try {
                Table.InsertAllOnSubmit(adds);
                if (UseAutoIncrement) {
                    RefreshContext();
                }
                else if (update) {
                    Update();
                }
                return false;
            }
            catch (Exception e) {
                Logger.error(e);
                return false;
            }
        }

        public void Delete(T del, bool update = true) {
            Table.DeleteOnSubmit(del);
            if (update) {
                Update();
            }
            DelEvent.OnNext(del);
        }
        public void DeleteAll(IEnumerable<T> dels, bool update=false) {
            Table.DeleteAllOnSubmit(dels);
            if (update) {
                Update();
            }
        }

        public void Update() {
            Table.Context.SubmitChanges();
        }

        public bool WithTransaction(Func<bool> fn) {
            using (var txn = this.Context.Connection.BeginTransaction()) {
                if (fn()) {
                    txn.Commit();
                    return true;
                }
                else {
                    txn.Dispose();
                    return false;
                }
            }
        }

        public void Dispose() {
            AddEvent.Dispose();
            DelEvent.Dispose();
            Context?.Dispose();
            Context = null;
            Table = null;
        }
    }
}
