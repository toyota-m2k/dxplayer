using dxplayer.data;
using dxplayer.data.main;
using dxplayer.data.wf;
using dxplayer.misc;
using dxplayer.player;
using dxplayer.server;
using dxplayer.settings;
using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using wfPlayer;

namespace dxplayer {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window, IStatusBar, IPlayListSource {
        static private LoggerEx logger = new LoggerEx("MainWindow");

        private MainViewModel ViewModel {
            get => (MainViewModel)DataContext;
            set => DataContext = value;
        }
        private DxServer mServer;

        #region Initialzing

        public MainWindow() {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
                Settings.Instance.Placement.ApplyPlacementTo(this);
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            logger.info("called");
            ViewModel = new MainViewModel();
            ViewModel.ImportFromWfCommand.Subscribe(ImportFromWf);
            ViewModel.AddFolderCommand.Subscribe(AddFolders);
            ViewModel.RefreshAllCommand.Subscribe(RefreshDB);
            ViewModel.PlayCommand.Subscribe(Play);

            ViewModel.PreviewCommand.Subscribe(Preview);
            ViewModel.CheckCommand.Subscribe(CheckItem);
            ViewModel.UncheckCommand.Subscribe(UncheckItem);
            ViewModel.ResetCounterCommand.Subscribe(ResetCounter);
            ViewModel.DeleteItemCommand.Subscribe(DeleteFiles);
            ViewModel.CopyItemPathCommand.Subscribe(CopyItemPath);
            ViewModel.DecrementCounterCommand.Subscribe(DecrementCounter);
            ViewModel.ShutdownCommand.Subscribe(Shutdown);

            ViewModel.SettingCommand.Subscribe(async () => {
                if(await ViewModel.Dialog.ShowSettingDialog()) {
                    mServer.Stop();
                    if(Settings.Instance.UseServer) {
                        mServer.Start(Settings.Instance.ServerPort);
                    }
                }
            });

            ViewModel.Dialog.SettingDialog.RefDxxDBPathCommand.Subscribe(() => {
                var path = OpenFileDialogBuilder.Create()
                    .addFileType("DxxBrowser DB", "*.db")
                    .defaultExtension("db")
                    .defaultFilename("dxxStorage")
                    .GetFilePath(this);
                if (path == null) return;
                ViewModel.Dialog.SettingDialog.DxxDBPath.Value = path;
            });

            ViewModel.HelpCommand.Subscribe(Help);

            Settings.Instance.SortInfo.SortUpdated += OnSortChanged;
            Settings.Instance.ListFilter.FilterUpdated += OnFilterChanged;

            MainListView.Focus();
            EnsureDB();
            OnSortChanged();

            ServerCommandCenter.Instance.MainWindow = this;
            mServer = new DxServer(this);
            if(Settings.Instance.UseServer) {
                mServer.Start(Settings.Instance.ServerPort);
            }
            ViewModel?.CommandManager?.Enable(this, true);

        }

        private KeyHelpWindow mHelpWindow = null;
        private void Help(object obj) {
            if (mHelpWindow != null) {
                return;
            }
            mHelpWindow = new KeyHelpWindow("Main", ViewModel.CommandManager.MakeHelpMessage());
            mHelpWindow.Closed += OnHelpWindowClosed;
            mHelpWindow.Show();
        }

        private void OnHelpWindowClosed(object sender, EventArgs e) {
            mHelpWindow = null;
        }


        #endregion

        #region Terminating

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            mHelpWindow?.Close();
            Settings.Instance.SortInfo.SortUpdated -= OnSortChanged;
            Settings.Instance.ListFilter.FilterUpdated -= OnFilterChanged;
            Settings.Instance.Placement.GetPlacementFrom(this);
            Settings.Instance.Serialize();
        }
        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            ViewModel.Dispose();
        }

        private void Shutdown() {
            mPlayerWindow?.Close();
            App.Instance.Shutdown();
            WinShutdown.Shutdown();
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
            this.Activate();
            //if(ViewModel.ListFilter.Enabled) {
            //    UpdateList();
            //}
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

        //private void Setting(object obj) {
        //    var path = OpenFileDialogBuilder.Create()
        //        .addFileType("DxxBrowser DB", "*.db")
        //        .defaultExtension("db")
        //        .defaultFilename("dxxStorage")
        //        .GetFilePath(this);
        //    if (path == null) return;
        //    if (Settings.Instance.DxxDBPath != path) {
        //        Settings.Instance.DxxDBPath = path;
        //        Settings.Instance.Serialize();
        //    }
        //}

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

        #region DB File

        private void CreateFileMenu() {
            var menu = new ContextMenu();
            var item = new MenuItem();
            item.Header = "_New DB";
            item.Command = new SimpleCommand(NewDB);
            menu.Items.Add(item);

            item = new MenuItem();
            item.Header = "_Open DB";
            item.Command = new SimpleCommand(OpenDB);
            menu.Items.Add(item);

            var curPath = Settings.Instance.FilePath;
            Settings.Instance.MRU.List.Where(c => c != curPath).Aggregate(menu, (m, path) => {
                var i = new MenuItem();
                var c = m.Items.Count - 1;
                if (c >= 10) {
                    c = 0;
                }
                i.Header = $"_{c}: {Path.GetFileName(path)}";
                i.Command = new SimpleCommand(() => OpenDB(path));
                m.Items.Add(i);
                return m;
            });
            if (menu.Items.Count > 2) {
                menu.Items.Insert(2, new Separator());
            }
            mOpenDBButton.DropDownMenu = menu;
        }

        private void EnsureDB() {
            if(OpenDB(Settings.Instance.FilePath)) {
                return;
            }
            while(!NewDB()) {
                MessageBox.Show("Select or create DB file.", "dxplayer", MessageBoxButton.OK);
            }
        }

        private bool OpenDB(string path) {
            if(App.Instance.OpenDB(path)) {
                CreateFileMenu();
                UpdateTitle();
                UpdateList();
                return true;
            }
            return false;
        }


        private bool OpenDB() {
            var path = OpenFileDialogBuilder.Create()
                .addFileType("dxplayer DB", "*.dpd")
                .defaultExtension("dpd")
                .GetFilePath(this);
            if (string.IsNullOrEmpty(path)) return false;
            return OpenDB(path);
        }

        private bool NewDB() {
            var path = SaveFileDialogBuilder.Create()
                .defaultExtension("dpd")
                .addFileType("dxplayer DB", "*.dpd")
                .GetFilePath(this);
            if (string.IsNullOrEmpty(path)) return false;
            return OpenDB(path);
        }

        /**
         * 選択されたDBファイル名をタイトルに表示する
         */
        private void UpdateTitle() {
            var dbg =
#if DEBUG
                "-dbg";
#else
                "";
#endif
            var path = Settings.Instance.FilePath;
            string fname = !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : "<untitled>";
            var v = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //Debug.WriteLine(v.ToString());
            this.Title = $"{v.ProductName}{dbg} (v{v.FileMajorPart}.{v.FileMinorPart}.{v.FileBuildPart}.{v.ProductPrivatePart})  - {fname}";
        }

#endregion

#region List Management

        public PlayItem SelectedItem {
            get => MainListView.SelectedItem as PlayItem;
            set {
                MainListView.SelectedItem = value;
                if (value != null) {
                    MainListView.ScrollIntoView(value);
                }
            }
        }
        public IEnumerable<PlayItem> SelectedItems => MainListView.SelectedItems.ToEnumerable<PlayItem>();
        public IEnumerable<PlayItem> ListedItems => MainListView.Items.ToEnumerable<PlayItem>();
        public IEnumerable<PlayItem> AllItems => DB.PlayListTable.List;

        public long CurrentId {
            get => SelectedItem?.ID ?? 0;
            set {
                var item = AllItems.Where(c => c.ID == value).SingleOrDefault();
                if(item!=null) {
                    MainListView.SelectedItem = item;
                    MainListView.ScrollIntoView(item);
                }
            }
        }

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

        private void DeleteFiles() {
            if(MessageBoxResult.OK != MessageBox.Show("Are you sure to delete selected file(s)?", "dxplayer", MessageBoxButton.OKCancel)) {
                return;
            }

            foreach (var c in SelectedItems) {
                c.Delete();
            }
            DB.PlayListTable.Update();
            UpdateList();
        }

        private void CopyItemPath() {
            var item = SelectedItem;
            if (item == null) return;

            Clipboard.SetText(item.Path);
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

        public void UpdateList() {
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
                //logger.debug(header.ToString());
                if (sorter.Shuffle) {
                    header.Tag = null;
                }
                else {
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
            logger.info("called");
            base.OnActivated(e);
            ViewModel?.CommandManager?.Enable(this, true);
            mPlayerWindow?.Activate();
        }

        protected override void OnDeactivated(EventArgs e) {
            logger.info("called");
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
