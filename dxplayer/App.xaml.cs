using dxplayer.data.main;
using dxplayer.settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
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

        protected override void OnStartup(StartupEventArgs e) {
            DB = MainStorage.OpenDB(Settings.Instance.FilePath);
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e) {
            base.OnExit(e);
            DB.Dispose();
        }

    }
}
