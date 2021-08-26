using dxplayer.data;
using dxplayer.data.main;
using dxplayer.data.wf;
using dxplayer.player;
using dxplayer.settings;
using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        #region Initialzing

        public MainWindow() {
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
            ViewModel.AddFolderCommand.Subscribe(AddFolders);
            ViewModel.RefreshAllCommand.Subscribe(RefreshDB);
            ViewModel.PlayCommand.Subscribe(Play);

            ViewModel.PreviewCommand.Subscribe(Preview);
            ViewModel.CheckCommand.Subscribe(CheckItem);
            ViewModel.UncheckCommand.Subscribe(UncheckItem);
            ViewModel.ResetCounterCommand.Subscribe(ResetCounter);
            ViewModel.DecrementCounterCommand.Subscribe(DecrementCounter);
            Settings.Instance.SortInfo.SortUpdated += OnSortChanged;
            Settings.Instance.ListFilter.FilterUpdated += OnFilterChanged;
            OnSortChanged();

            MainListView.Focus();
        }

        #endregion

        #region Terminating

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            Settings.Instance.SortInfo.SortUpdated -= OnSortChanged;
            Settings.Instance.ListFilter.FilterUpdated -= OnFilterChanged;
            Settings.Instance.Placement.GetPlacementFrom(this);
            Settings.Instance.Serialize();
        }
        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            ViewModel.Dispose();
        }
        #endregion

        #region PlayerWindow Management

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
                if (index >= 0) {
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

        private void OnListItemDoubleClick(object sender, MouseButtonEventArgs e) {
            Preview();
        }

        #endregion

        #region Settings

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

        #endregion

        #region StatusBar Messages

        public void OutputStatusMessage(string msg) {
            Dispatcher.Invoke(() => {
                ViewModel.StatusMessage.Value = msg;
            });
        }

        public void FlashStatusMessage(string msg, int duration = 5/*sec*/) {
            Dispatcher.Invoke(async () => {
                ViewModel.StatusMessage.Value = msg;
                await Task.Delay(duration * 1000);
                if (ViewModel.StatusMessage.Value == msg) {
                    ViewModel.StatusMessage.Value = "";
                }
            });
        }

        #endregion

        #region DB Management

        private MainStorage DB => App.Instance.DB;

        private async void ImportFromWf() {
            var path = OpenFileDialogBuilder.Create()
                .addFileType("wfPlayer DB", "*.wpd")
                .defaultExtension("wpd")
                .defaultFilename("default")
                .GetFilePath(this);
            if (string.IsNullOrEmpty(path)) return;
            using (WaitCursor.Start(this))
            using (var wfdb = WfStorage.SafeOpen(path)) {
                if (wfdb == null) return;
                await wfdb.ExportTo(DB, this);
            }
            UpdateList();
        }

        private async void AddFolders() {
            var path = FolderDialogBuilder
                            .Create()
                            .title("Select Folder")
                            .GetFilePath(this);
            if (string.IsNullOrEmpty(path)) return;

            using (WaitCursor.Start(this)) {
                await DB.AddTargetFolder(path, this);
            }
            UpdateList();
        }

        private async void RefreshDB() {
            using (WaitCursor.Start(this)) {
                await DB.RefreshDB(this);
            }
            UpdateList();
        }

        #endregion

        #region List Management

        public PlayItem SelectedItem => MainListView.SelectedItem as PlayItem;
        public IEnumerable<PlayItem> SelectedItems => MainListView.SelectedItems.ToEnumerable<PlayItem>();
        public IEnumerable<PlayItem> ListedItems => MainListView.Items.ToEnumerable<PlayItem>();
        public IEnumerable<PlayItem> AllItems => DB.PlayListTable.List;

        private void DecrementCounter() {
            foreach(var c in SelectedItems) {
                c.PlayCount = Math.Max(0, c.PlayCount - 1);
            }
            DB.PlayListTable.Update();
        }

        private void ResetCounter() {
            foreach (var c in SelectedItems) {
                c.PlayCount = 0;
            }
            DB.PlayListTable.Update();
        }

        private void UncheckItem() {
            foreach (var c in SelectedItems) {
                c.Checked = false;
            }
            DB.PlayListTable.Update();
        }

        private void CheckItem() {
            foreach (var c in SelectedItems) {
                c.Checked = true;
            }
            DB.PlayListTable.Update();
        }


        #endregion

        #region Sort / Filter

        private void UpdateList() {
            var list = DB.PlayListTable.List.Filter();
            if (Settings.Instance.SortInfo.Shuffle) {
                ViewModel.MainList.Value = Shuffle(list);
            }
            else {
                ViewModel.MainList.Value = new ObservableCollection<PlayItem>(list.Sort());
            }
        }

        private void OnSortChanged() {
            UpdateColumnHeaderOnSort();
            UpdateList();
        }

        private void OnFilterChanged() {
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

        #region View Events

        //private void OnKeyDown(object sender, KeyEventArgs e) {
        //    LoggerEx.debug($"Key={e.Key}, Sys={e.SystemKey}, State={e.KeyStates}, Rep={e.IsRepeat}, Down={e.IsDown}, Up={e.IsUp}, Toggled={e.IsToggled}");
        //    //ViewModel.CommandManager.Down(e.Key);
        //}

        //private void OnKeyUp(object sender, KeyEventArgs e) {
        //    LoggerEx.debug($"Key={e.Key}, Sys={e.SystemKey}, State={e.KeyStates}, Rep={e.IsRepeat}, Down={e.IsDown}, Up={e.IsUp}, Toggled={e.IsToggled}");
        //    //ViewModel.CommandManager.Up(e.Key);
        //}

        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);
            ViewModel.CommandManager.Enable(this, true);
            mPlayerWindow?.Activate();
        }

        protected override void OnDeactivated(EventArgs e) {
            base.OnDeactivated(e);
            ViewModel.CommandManager.Enable(this, false);
        }

        #endregion

        //private void OnKeyDown2(object sender, KeyEventArgs e) {
        //    LoggerEx.debug($"Key={e.Key}, Sys={e.SystemKey}, State={e.KeyStates}, Rep={e.IsRepeat}, Down={e.IsDown}, Up={e.IsUp}, Toggled={e.IsToggled}");
        //}

        //private void OnKeyUp2(object sender, KeyEventArgs e) {
        //    LoggerEx.debug($"Key={e.Key}, Sys={e.SystemKey}, State={e.KeyStates}, Rep={e.IsRepeat}, Down={e.IsDown}, Up={e.IsUp}, Toggled={e.IsToggled}");
        //}
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
