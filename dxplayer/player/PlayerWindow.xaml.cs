using dxplayer.settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace dxplayer.player
{
    /// <summary>
    /// PlayerWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class PlayerWindow : Window {
        public PlayerViewModel ViewModel {
            get => DataContext as PlayerViewModel;
            set {
                ViewModel?.Dispose();
                DataContext = value;
            }
        }


        public PlayerWindow(bool checkMode) {
            ViewModel = new PlayerViewModel(checkMode);
            InitializeComponent();
        }

        public event Action<IPlayItem> PlayItemChanged;
        public event Action<PlayerWindow> PlayWindowClosing;
        public event Action<PlayerWindow> PlayWindowClosed;

        private TaskCompletionSource<bool> LoadCompletion = new TaskCompletionSource<bool>();

        //public IPlayList PlayList => Player.ControlPanel.PlayList;

        public (IPlayItem entry, double position) CurrentPlayingInfo {
            get {
                var entry = ViewModel.PlayList.Current.Value;
                double position = 0;
                if(ViewModel.IsReady.Value) {
                    position = Player.SeekPosition;
                }
                return (entry, position);
            }
        }

        public void ResumePlay(IEnumerable<IPlayItem> list, IPlayItem entry/*, double pos*/) {
            if (entry != null) {
                ViewModel.PlayList.SetList(list, entry);
                //Player.ReserveSeekPosition(pos);
            }
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            Settings.Instance.PlayerPlacement.ApplyPlacementTo(this);
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ViewModel.CommandManager.Enable(this, true);
            ViewModel.PlayList.Current.Subscribe(OnCurrentItemChanged);
            ViewModel.ClosePlayerCommand.Subscribe(Close);
            LoadCompletion.TrySetResult(true);
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);
            Settings.Instance.PlayerPlacement.GetPlacementFrom(this);
            ViewModel.ChapterEditor.SaveChapterListIfNeeds();
            ViewModel.PlayList.ResetList();     // 最後に再生中のアイテムについて、PlayCountを更新するため、Currentを変化させて、PlayCountObserverに、ItemChangedイベントを受け取らせたい。
            LoadCompletion.TrySetResult(false);
            PlayWindowClosing?.Invoke(this);
            App.Instance.DB.ChapterTable.Update();
            App.Instance.DB.PlayListTable.Update();
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            PlayWindowClosed?.Invoke(this);
            PlayWindowClosed = null;
            PlayItemChanged = null;
            ViewModel = null;
        }

        //private void Window_PreviewDragOver(object sender, DragEventArgs e) {
        //    e.Effects = DragDropEffects.Copy;
        //}

        //private void Window_Drop(object sender, DragEventArgs e) {
        //    MainWindow.Instance?.RegisterUrl(e.Data.GetData(DataFormats.Text) as string, true);
        //}

        public async void SetPlayList(IEnumerable<IPlayItem> s, IPlayItem initialItem = null) {
            await LoadCompletion.Task;
            ViewModel.PlayList.SetList(s, initialItem);
        }

        public async void AddToPlayList(IPlayItem item) {
            await LoadCompletion.Task;
            ViewModel.PlayList.Add(item);
        }

        private DispatcherTimer FlashTitleTimer = null;
        private void OnCurrentItemChanged(IPlayItem item) {
            FlashTitleTimer?.Stop();
            this.Title = item?.TitleOrName() ?? "";
            if(item!=null && ViewModel.Fullscreen.Value && !string.IsNullOrWhiteSpace(item.Title)) {
                ViewModel.ShowLabelPanel.Value = true;
                if (FlashTitleTimer == null) {
                    FlashTitleTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(5) };
                    FlashTitleTimer.Tick += (s,e) => {
                        FlashTitleTimer.Stop();
                        ViewModel.ShowLabelPanel.Value = false;
                    };
                }
                FlashTitleTimer.Start();
            }
            PlayItemChanged?.Invoke(item);
        }

        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);
            ViewModel.CommandManager.Enable(this, true);
        }

        protected override void OnDeactivated(EventArgs e) {
            base.OnDeactivated(e);
            ViewModel.CommandManager.Enable(this, false);
        }

        //private void AddTextToRichEdit(string text, Brush fg) {
        //    var p = OutputView.Document.Blocks.FirstBlock as Paragraph;
        //    var run = new Run(text);
        //    if (fg != null) {
        //        run.Foreground = fg;
        //    }
        //    p.Inlines.Add(run);
        //    p.Inlines.Add(new LineBreak());
        //    OutputView.ScrollToEnd();
        //}

        //private void StandardOutput(string text) {
        //    LoggerEx.info(text);
        //    StandardError(text);
        //}
        //private void StandardError(string text) {
        //    Dispatcher.Invoke(() => {
        //        LoggerEx.error(text);
        //        Brush brush = null;
        //        if (text.StartsWith("size=")) {
        //            brush = new SolidColorBrush(Colors.Green);
        //        } else if (text.ToLower().Contains("error")) {
        //            brush = new SolidColorBrush(Colors.Red);
        //        }
        //        AddTextToRichEdit(text, brush);
        //    });
        //}


        //private async void OnAutoChapter() {
        //    if (!ViewModel.ChapterEditing.Value) return;
        //    var item = ViewModel.PlayList.Current.Value;
        //    if (item == null) return;
        //    var chapterList = ViewModel.Chapters.Value;
        //    if (chapterList.Values.Count > 0) {
        //        if (MessageBoxResult.OK != MessageBox.Show(GetWindow(this), "All chapters will be replaced with created chapters.", "Auto Chapter", MessageBoxButton.OKCancel)) {
        //            return;
        //        }
        //    }

        //    OutputView.Visibility = Visibility.Visible;
        //    OutputView.Document = new FlowDocument();
        //    OutputView.Document.Blocks.Add(new Paragraph());

        //    try {

        //        if (ViewModel.WavFile == null) {
        //            AddTextToRichEdit("Extracting sound track...", new SolidColorBrush(Colors.Blue));
        //            try {
        //                var outFile = Path.Combine(Settings.Instance.EnsureWorkPath, "x.wav");
        //                PathUtil.safeDeleteFile(outFile);

        //                using (WaitCursor.Start(this)) {
        //                    ViewModel.WavFile = await WavFile.CreateFromMP4(item.VPath, outFile, StandardOutput, StandardError);
        //                    if (ViewModel.WavFile == null) {
        //                        MessageBox.Show(GetWindow(this), "Cannot extract sound track from MP4 file.", "Auto Chapter", MessageBoxButton.OK);
        //                        return;
        //                    }
        //                }
        //            }
        //            catch (Exception e) {
        //                LoggerEx.error(e);
        //            }
        //        }

        //        AddTextToRichEdit("Analyzing sound track...", new SolidColorBrush(Colors.Blue));

        //        T limit<IT, T>(IT v, T min, T max) {
        //            if ((dynamic)v < (dynamic)min) return min;
        //            if ((dynamic)max < (dynamic)v) return max;
        //            return (T)(dynamic)v;
        //        }

        //        var wavFile = ViewModel.WavFile;
        //        short threshold = limit(ViewModel.AutoChapterThreshold.Value, (short)0, (short)5000);
        //        double span = limit((double)(ViewModel.AutoChapterSpan.Value) / 1000, (double)0.5, (double)5.0);
        //        var result = await Task<bool>.Run(() => {
        //            var ranges = wavFile.ScanChapter(threshold, span);

        //            if (!Utils.IsNullOrEmpty(ranges)) {
        //                Dispatcher.Invoke(() => {
        //                    chapterList.ClearAllChapters();
        //                });
        //                foreach (var r in ranges) {
        //                    var d = r.Item2 - r.Item1;
        //                    var p = r.Item2 - Math.Min(d / 2, 1.0);
        //                    var pos = (ulong)Math.Round(p * 1000);
        //                    Dispatcher.Invoke(() => {
        //                        chapterList.AddChapter(new ChapterInfo(pos));
        //                        AddTextToRichEdit($"  chapter-{chapterList.Values.Count} : {pos} msec", new SolidColorBrush(Colors.Gray));
        //                    });
        //                }
        //                return true;
        //            } else {
        //                return false;
        //            }
        //        });
        //        if (result) {
        //            ViewModel.NotifyChapterUpdated();
        //        } else {
        //            AddTextToRichEdit($"No chapter was detected.", new SolidColorBrush(Colors.Red));
        //            MessageBox.Show(GetWindow(this), "No chapter was detected.", "Auto Chapter", MessageBoxButton.OK);
        //        }
        //    }
        //    finally {
        //        await Task.Delay(1000);
        //        OutputView.Visibility = Visibility.Hidden;
        //        OutputView.Document.Blocks.Clear();
        //    }
        //}
    }
}
