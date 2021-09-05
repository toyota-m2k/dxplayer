using dxplayer.data.main;
using dxplayer.settings;
using System.Windows;

namespace dxplayer
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static App Instance => Current as App;
        public MainStorage DB { get; private set; }

        public bool OpenDB(string path) {
            var db = MainStorage.OpenDB(path);
            if (db == null) return false;
            DB?.Dispose();
            DB = db;
            Settings.Instance.FilePath = path;
            Settings.Instance.MRU.AddMru(path);
            Settings.Instance.Serialize();
            return true;
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e) {
            base.OnExit(e);
            DB.Dispose();
            Settings.Instance.Serialize();
        }
    }
}
