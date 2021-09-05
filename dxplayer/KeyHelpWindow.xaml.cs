using dxplayer.misc;
using dxplayer.settings;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace dxplayer {
    /// <summary>
    /// KeyHelpWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class KeyHelpWindow : Window {
        public string HelpKey { get; }
        public ObservableCollection<KeyMapHelpItem> HelpItems { get; }

        public KeyHelpWindow(string helpKey, IEnumerable<KeyMapHelpItem> helpItems) {
            HelpKey = helpKey;
            HelpItems = new ObservableCollection<KeyMapHelpItem>(helpItems);
            DataContext = this;
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            var placement = Settings.Instance.HelpPlacement.GetValue(HelpKey);
            if (placement != null) {
                placement.ApplyPlacementTo(this);
            }
        }
        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            var placement = new WinPlacement();
            placement.GetPlacementFrom(this);
            Settings.Instance.HelpPlacement[HelpKey] = placement;
        }
    }
}
