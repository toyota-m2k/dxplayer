using dxplayer.data;
using dxplayer.data.main;
using dxplayer.data.wf;
using dxplayer.player;
using dxplayer.settings;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace dxplayer {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window, IStatusBar {
        private MainViewModel ViewModel {
            get => (MainViewModel)DataContext;
            set => DataContext = value;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            Settings.Instance.Placement.ApplyPlacementTo(this);
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ViewModel = new MainViewModel();
            ViewModel.SettingCommand.Subscribe(Setting);
            ViewModel.ImportFromWfCommand.Subscribe(ImportFromWf);
            ViewModel.PlayCommand.Subscribe(Play);
            ViewModel.PreviewCommand.Subscribe(Preview);
            Settings.Instance.SortInfo.SortUpdated += OnSortChanged;
            Settings.Instance.ListFilter.FilterUpdated += OnFilterChanged;
            OnSortChanged();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {

        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            Settings.Instance.SortInfo.SortUpdated -= OnSortChanged;
            Settings.Instance.ListFilter.FilterUpdated -= OnFilterChanged;
            Settings.Instance.Placement.GetPlacementFrom(this);
            Settings.Instance.Serialize();
        }

        private void OnFilterChanged() {
            UpdateList();
        }

        private void OnSortChanged() {
            UpdateColumnHeaderOnSort();
            UpdateList();
        }

        private PlayerWindow mPlayerWindow = null;
        private PlayCountObserver mPlayCountObserver = null;
        private PlayerWindow OpenPlayer(bool preview) {
            if (mPlayerWindow == null) {
                mPlayerWindow = new PlayerWindow(preview);
                mPlayerWindow.PlayItemChanged += OnPlayItemChanged;
                mPlayerWindow.PlayWindowClosing += OnPlayerWindowClosing;
                mPlayerWindow.PlayWindowClosed += OnPlayerWindowClosed;
                mPlayerWindow.Owner = this;
                ViewModel.PlayingStatus.Value = preview ? PlayingStatus.CHECKING : PlayingStatus.PLAYING;
                if (!preview) {
                    mPlayCountObserver = new PlayCountObserver(mPlayerWindow.ViewModel);
                }
                mPlayerWindow.Show();
            }
            return mPlayerWindow;
        }

        private void OnPlayerWindowClosing(PlayerWindow obj) {
        }

        private void OnPlayItemChanged(IPlayItem obj) {
            var item = obj as PlayItem;
            if (item != null) {
                var index = ViewModel.MainList.Value.IndexOf(item);
                if(index>=0) {
                    MainListView.SelectedIndex = index;
                    MainListView.ScrollIntoView(item);
                }
            }
        }

        private void OnPlayerWindowClosed(PlayerWindow obj) {
            if (mPlayerWindow != null) {
                mPlayerWindow.PlayItemChanged -= OnPlayItemChanged;
                mPlayerWindow.PlayWindowClosing -= OnPlayerWindowClosing;
                mPlayerWindow.PlayWindowClosed -= OnPlayerWindowClosed;
                mPlayerWindow = null;
            }
            ViewModel.PlayingStatus.Value = PlayingStatus.IDLE;
            mPlayCountObserver?.Dispose();
            mPlayCountObserver = null;
        }

        private void Play(bool preview) {
            var selected = MainListView.SelectedItems;
            var player = OpenPlayer(preview);
            if (selected.Count > 1) {
                player.SetPlayList(selected.ToEnumerable<PlayItem>());
            }
            else {
                player.SetPlayList(ViewModel.MainList.Value, MainListView.SelectedItem as PlayItem);
            }
        }

        private void Play() {
            Play(false);
        }

        private void Preview() {
            Play(true);
        }



        private void Setting(object obj) {
            var path = OpenFileDialogBuilder.Create()
                .addFileType("DxxBrowser DB", "*.db")
                .defaultExtension("db")
                .defaultFilename("dxxStorage")
                .GetFilePath(this);
            if (path == null) return;
            if (Settings.Instance.DxxDBPath != path) {
                Settings.Instance.DxxDBPath = path;
                Settings.Instance.Serialize();
            }
        }

        #region StatusBar Messages

        public void OutputStatusMessage(string msg) {
            Dispatcher.Invoke(() => {
                ViewModel.StatusMessage.Value = msg;
            });
        }

        public void FlashStatusMessage(string msg, int duration=5/*sec*/) {
            Dispatcher.Invoke(async () => {
                ViewModel.StatusMessage.Value = msg;
                await Task.Delay(duration * 1000);
                if(ViewModel.StatusMessage.Value == msg) {
                    ViewModel.StatusMessage.Value = "";
                }
            });
        }

        #endregion

        private async void ImportFromWf(object obj) {
            var path = OpenFileDialogBuilder.Create()
                .addFileType("wfPlayer DB", "*.wpd")
                .defaultExtension("wpd")
                .defaultFilename("default")
                .GetFilePath(this);

            using (var wfdb = WfStorage.SafeOpen(path)) {
                if (wfdb == null) return;
                await wfdb.ExportTo(App.Instance.DB, this);
            }
            UpdateList();
        }

        public ObservableCollection<PlayItem> Shuffle(IEnumerable<PlayItem> src) {
            //Fisher-Yatesアルゴリズムでシャッフルする
            var list = src.ToList();
            Random rnd = new Random();
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rnd.Next(n + 1);
                var tmp = list[k];
                list[k] = list[n];
                list[n] = tmp;
            }
            return new ObservableCollection<PlayItem>(list);
        }


        private void UpdateList() {
            var list = App.Instance.DB.PlayListTable.List.Filter();
            if (Settings.Instance.SortInfo.Shuffle) {
                ViewModel.MainList.Value = Shuffle(list);
            } else {
                ViewModel.MainList.Value = new ObservableCollection<PlayItem>(list.Sort());
            }
        }

        #region Sort

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject {
            if (depObj != null) {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T) {
                        yield return (T)child;
                    }
                    foreach (T childOfChild in FindVisualChildren<T>(child)) {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void UpdateColumnHeaderOnSort() {
            var sorter = Settings.Instance.SortInfo;
            foreach (var header in FindVisualChildren<GridViewColumnHeader>(MainListView)) {
                LoggerEx.debug(header.ToString());
                var textBox = FindVisualChildren<TextBlock>(header).FirstOrDefault();
                if (null != textBox) {
                    var key = SortInfo.SortKeyFromString(textBox.Text);
                    if (key == sorter.PrimaryKey) {
                        header.Tag = sorter.Order == SortInfo.SortOrder.ASCENDING ? "asc" : "desc";
                    }
                    else {
                        header.Tag = null;
                    }
                }
            }
        }


        private void OnHeaderClick(object sender, RoutedEventArgs e) {
            var header = e.OriginalSource as GridViewColumnHeader;
            if (null == header) {
                return;
            }
            Settings.Instance.SortInfo.SetSortKey(header.Content.ToString());
        }
        #endregion

        private void OnListItemDoubleClick(object sender, MouseButtonEventArgs e) {
            Preview();
        }
    }

    public static class LinqExt {
        //public static IEnumerable<PlayItem> FilterByRating(this IEnumerable<PlayItem> s, RatingFilter rf) {
        //    return rf.Filter(s);
        //}
        //public static IEnumerable<PlayItem> FilterByCategory(this IEnumerable<PlayItem> s, Category c) {
        //    return c.Filter(s);
        //}
        public static IEnumerable<PlayItem> Filter(this IEnumerable<PlayItem> s) {
            return Settings.Instance.ListFilter.Filter(s);
        }
        public static IOrderedEnumerable<PlayItem> Sort(this IEnumerable<PlayItem> s) {
            return Settings.Instance.SortInfo.Sort(s);
        }
    }

}
