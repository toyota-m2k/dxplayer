using dxplayer.data;
using dxplayer.data.main;
using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;

namespace dxplayer.player {
    public enum PlayerState {
        UNAVAILABLE,
        LOADING,
        READY,
        PLAYING,
        ENDED,
        ERROR,
    }
    public enum PanelPosition {
        LEFT,
        RIGHT,
        TOP,
        BOTTOM,
    }
    public class PlayerViewModel : ViewModelBase {
        #region Control Panel Position
        const double DEF_PANEL_WIDTH = 320;
        public ReactivePropertySlim<PanelPosition> PanelPosition { get; } = new ReactivePropertySlim<PanelPosition>(player.PanelPosition.RIGHT);
        public ReadOnlyReactivePropertySlim<HorizontalAlignment> PanelHorzAlign { get; }
        public ReadOnlyReactivePropertySlim<VerticalAlignment> PanelVertAlign { get; }
        public ReadOnlyReactivePropertySlim<double> PanelWidth { get; }
        #endregion

        #region Properties of Item Entry

        public ReactivePropertySlim<ulong> Duration { get; } = new ReactivePropertySlim<ulong>(1000);
        public ReactivePropertySlim<ulong> Position { get; } = new ReactivePropertySlim<ulong>(0);
        public ReactivePropertySlim<PlayerState> State { get; } = new ReactivePropertySlim<PlayerState>(PlayerState.UNAVAILABLE);

        public ReadOnlyReactivePropertySlim<bool> IsReady { get; }
        public ReadOnlyReactivePropertySlim<bool> IsPlaying { get; }

        public ReactivePropertySlim<double> Speed { get; } = new ReactivePropertySlim<double>(0.5);
        public ReactivePropertySlim<double> Volume { get; } = new ReactivePropertySlim<double>(0.5);
        public ReactivePropertySlim<bool> Mute { get; } = new ReactivePropertySlim<bool>(false);

        #endregion

        #region Trimming/Chapters

        public ReactivePropertySlim<PlayRange> Trimming { get; } = new ReactivePropertySlim<PlayRange>(PlayRange.Empty);
        public ReactivePropertySlim<ChapterList> Chapters { get; } = new ReactivePropertySlim<ChapterList>(null, ReactivePropertyMode.RaiseLatestValueOnSubscribe);
        public ReactivePropertySlim<List<PlayRange>> DisabledRanges { get; } = new ReactivePropertySlim<List<PlayRange>>(null);
        public ReadOnlyReactivePropertySlim<bool> HasDisabledRange { get; }
        public ReadOnlyReactivePropertySlim<bool> HasTrimming { get; }
        public ReactivePropertySlim<bool> ChapterEditing { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<ObservableCollection<ChapterInfo>> EditingChapterList { get; } = new ReactivePropertySlim<ObservableCollection<ChapterInfo>>();
        public Subject<string> ReachRangeEnd { get; } = new Subject<string>();

        public ReactiveCommand<ulong> NotifyPosition { get; } = new ReactiveCommand<ulong>();
        public ReactiveCommand<PlayRange> NotifyRange { get; } = new ReactiveCommand<PlayRange>();
        public ReactivePropertySlim<PlayRange?> DraggingRange { get; } = new ReactivePropertySlim<PlayRange?>(null);

        /**
         * 現在再生中の動画のチャプター設定が変更されていればDBに保存する。
         */
        public void SaveChapterListIfNeeds() {
            var storage = App.Instance.DB;
            if (null == storage) return;
            var item = PlayList.Current.Value;
            if (item == null) return;
            var chapterList = Chapters.Value;
            if (chapterList == null || !chapterList.IsModified) return;
            storage.ChapterTable.UpdateByChapterList(chapterList);
        }

        /**
         * 再生する動画のチャプターリスト、トリミング情報、無効化範囲リストを準備する。
         */
        public void PrepareChapterListForCurrentItem() {
            //Chapters.Value = null;
            //DisabledRanges.Value = null;
            //Trimming.Value = null;
            var item = PlayList.Current.Value;
            if (item == null) return;
            Trimming.Value = new PlayRange(item.TrimStart, item.TrimEnd);
            var chapterList = App.Instance.DB.ChapterTable.GetChapterList(item.Path);
            if (chapterList != null) {
                Chapters.Value = chapterList;
                DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
            } else {
                DisabledRanges.Value = new List<PlayRange>();
            }
            if (ChapterEditing.Value) {
                EditingChapterList.Value = Chapters.Value.Values;
            }
        }

        public void CurrentToTrimmingStart() {
            SetTrimming(SetTrimmingStart);
        }
        public void CurrentToTrimmingEnd() {
            SetTrimming(SetTrimmingEnd);
        }
        public void ResetTrimmingStart() {
            ResetTrimming(SetTrimmingStart);
        }
        public void ResetTrimmingEnd() {
            ResetTrimming(SetTrimmingEnd);
        }

        private void SetTrimming(object obj) {
            switch (obj as String) {
                case "Start": CurrentToTrimmingStart(); break;
                case "End": CurrentToTrimmingEnd(); break;
                default: return;
            }
        }
        private void ResetTrimming(object obj) {
            switch (obj as String) {
                case "Start": ResetTrimmingStart(); break;
                case "End": ResetTrimmingEnd(); break;
                default: return;
            }
        }

        private void ResetTrimming(Func<IPlayItem, ulong, PlayRange?> setFunc) {
            var item = PlayList.Current.Value;
            if (item == null) return;
            var trimming = setFunc(item, 0);
            if (trimming == null) return;

            Trimming.Value = trimming.Value;
            DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming.Value).ToList();
        }

        private void SetTrimming(Func<IPlayItem, ulong, PlayRange?> setFunc) {
            var item = PlayList.Current.Value;
            if (item == null) return;
            var pos = PlayerPosition;
            var trimming = setFunc(item, pos);
            if (trimming == null) return;

            Trimming.Value = trimming.Value;
            DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming.Value).ToList();
        }
        private PlayRange? SetTrimmingStart(IPlayItem item, ulong pos) {
            var trimming = Trimming.Value;
            if (trimming.TrySetStart(pos)) {
                item.TrimStart = pos;
                return trimming;
            }
            return null;
        }
        private PlayRange? SetTrimmingEnd(IPlayItem item, ulong pos) {
            var trimming = Trimming.Value;
            if (trimming.TrySetEnd(pos)) {
                item.TrimEnd = pos;
                return trimming;
            }
            return null;
        }

        public void NotifyChapterUpdated() {
            Chapters.Value.Apply((chapterList) => {
                Chapters.Value = chapterList;
                DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
            });
        }

        private void AddChapter() {
            AddChapter(PlayerPosition);
        }

        private void AddChapter(ulong pos) {
            if (PlayList.Current.Value == null) return;
            var chapterList = Chapters.Value;
            if (chapterList == null) return;
            if (pos > Duration.Value) return;
            if (chapterList.AddChapter(new ChapterInfo(pos))) {
                Chapters.Value = chapterList;
                DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
            }
        }

        private void AddDisabledChapterRange(PlayRange range) {
            if (PlayList.Current.Value == null) return;
            var chapterList = Chapters.Value;
            if (chapterList == null) return;

            range.AdjustTrueEnd(Duration.Value);
            var del = chapterList.Values.Where(c => range.Start <= c.Position && c.Position <= range.End).ToList();
            foreach (var e in del) {    // chapterList.Valuesは ObservableCollection なので、RemoveAll的なやつ使えない。
                chapterList.Values.Remove(e);
            }
            chapterList.AddChapter(new ChapterInfo(range.Start) { Skip = true });
            if (range.End != Duration.Value) {
                chapterList.AddChapter(new ChapterInfo(range.End));
            }
            Chapters.Value = chapterList;
            DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
        }

        private void NextChapter() {
            var chapterList = Chapters.Value;
            chapterList.GetNeighbourChapterIndex(PlayerPosition, out var prev, out var next);
            if (next >= 0) {
                var c = chapterList.Values[next].Position;
                if (Trimming.Value.Contains(c)) {
                    Position.Value = c;
                    return;
                }
            }
            GoForwardCommand.Execute();
        }

        private void PrevChapter() {
            var chapterList = Chapters.Value;
            var basePosition = PlayerPosition;
            if (basePosition > 1000)
                basePosition -= 1000;
            chapterList.GetNeighbourChapterIndex(basePosition, out var prev, out var next);
            if (prev >= 0) {
                var c = chapterList.Values[prev].Position;
                if (Trimming.Value.Contains(c)) {
                    Position.Value = c;
                    return;
                }
            }
            Position.Value = Trimming.Value.Start;
        }

        #endregion

        #region Display Text

        public ReadOnlyReactivePropertySlim<string> TrimStartText { get; }
        public ReadOnlyReactivePropertySlim<string> TrimEndText { get; }
        public ReadOnlyReactivePropertySlim<string> DurationText { get; }
        public ReadOnlyReactivePropertySlim<string> PositionText { get; }

        static public string FormatDuration(ulong duration) {
            var t = TimeSpan.FromMilliseconds(duration);
            return string.Format("{0}:{1:00}:{2:00}", t.Hours, t.Minutes, t.Seconds);
        }

        #endregion

        #region Commands

        public ReactiveCommand PlayCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PauseCommand { get; } = new ReactiveCommand();
        //public ReactiveCommand StopCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoBackCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoForwardCommand { get; } = new ReactiveCommand();
        public ReactiveCommand TrashCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetSpeedCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetVolumeCommand { get; } = new ReactiveCommand();
        public ReactiveCommand AddChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PrevChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand NextChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SyncChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand<string> PanelPositionCommand { get; } = new ReactiveCommand<string>();
        public ReactiveCommand SetTrimCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetTrimCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CheckedCommand { get; } = new ReactiveCommand();
        public ReactiveCommand TrimmingToChapterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ClosePlayerCommand { get; } = new ReactiveCommand();
        public ReactiveCommand KickOutMouseCommand { get; } = new ReactiveCommand();



        #endregion

        #region Window Managements

        // Window
        public ReactivePropertySlim<bool> FitMode { get; } = new ReactivePropertySlim<bool>();
        public ReactivePropertySlim<bool> Fullscreen { get; } = new ReactivePropertySlim<bool>(false);

        public ReactivePropertySlim<bool> ReqShowControlPanel { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> ReqShowSizePanel { get; } = new ReactivePropertySlim<bool>(false);
        public ReactivePropertySlim<bool> PinControlPanel { get; } = new ReactivePropertySlim<bool>(false);

        public ReadOnlyReactivePropertySlim<bool> ShowControlPanel { get; }
        public ReadOnlyReactivePropertySlim<bool> ShowSizePanel { get; }
        public ReadOnlyReactivePropertySlim<bool> MinimumPanel { get; }
        public ReadOnlyReactivePropertySlim<bool> CursorManagerActivity { get; }

        public ReactiveCommand MaximizeCommand { get; } = new ReactiveCommand();
        public bool CheckMode { get; }

        public PlayerCommands CommandManager { get; }

        #endregion

        #region PlayList

        public PlayList PlayList { get; } = new PlayList();
        public bool AutoPlay { get; } = true;

        #endregion

        #region Reference to Player
        // Player
        private WeakReference<Player> mPlayer = null;
        public Player Player {
            get => mPlayer?.GetValue();
            set { mPlayer = value == null ? null : new WeakReference<Player>(value); }
        }
        public ulong PlayerPosition {
            get => (ulong)(Player?.SeekPosition ?? 0);
            set { Player?.Apply((player) => player.SeekPosition = value); }
        }

        #endregion

        #region Construction/Destruction

        public PlayerViewModel(bool checkMode) {
            CheckMode = checkMode;
            AutoPlay = !checkMode;
            DurationText = Duration.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();
            PositionText = Position.Select((v) => FormatDuration(v)).ToReadOnlyReactivePropertySlim();
            TrimStartText = Trimming.Select((v) => FormatDuration(v.Start)).ToReadOnlyReactivePropertySlim();
            TrimEndText = Trimming.Select((v) => FormatDuration(v.End)).ToReadOnlyReactivePropertySlim();
            HasDisabledRange = DisabledRanges.Select((c) => c != null && c.Count > 0).ToReadOnlyReactivePropertySlim();
            HasTrimming = Trimming.Select(c => c.Start > 0 || c.End > 0).ToReadOnlyReactivePropertySlim();
            IsPlaying = State.Select((v) => v == PlayerState.PLAYING).ToReadOnlyReactivePropertySlim();
            IsReady = State.Select((v) => v == PlayerState.READY || v == PlayerState.PLAYING).ToReadOnlyReactivePropertySlim();

            ShowSizePanel = ReqShowSizePanel.ToReadOnlyReactivePropertySlim();
            ShowControlPanel = ReqShowControlPanel.CombineLatest(PinControlPanel, ChapterEditing, (req, pin, editing) => {
                return CheckMode || req || pin || editing;
            }).ToReadOnlyReactivePropertySlim();
            MinimumPanel = PinControlPanel.CombineLatest(IsPlaying, ChapterEditing, ReqShowControlPanel, (pin, playing, editing, req) => {
                return pin && playing && !CheckMode && !req && !editing;
            }).ToReadOnlyReactivePropertySlim();
            CursorManagerActivity = ShowControlPanel.CombineLatest(ShowSizePanel, (c, s) => !c && !s).ToReadOnlyReactivePropertySlim();

            GoForwardCommand.Subscribe(() => {
                if (ChapterEditing.Value) {
                    EditingChapterList.Value = null;
                    SaveChapterListIfNeeds();
                }
                PlayList.Next();
            });
            GoBackCommand.Subscribe(() => {
                if (ChapterEditing.Value) {
                    EditingChapterList.Value = null;
                    SaveChapterListIfNeeds();
                }
                PlayList.Prev();
            });
            TrashCommand.Subscribe(PlayList.DeleteCurrent);
            ResetSpeedCommand.Subscribe(() => Speed.Value = 0.5);
            ResetVolumeCommand.Subscribe(() => Volume.Value = 0.5);

            SetTrimCommand.Subscribe(SetTrimming);
            ResetTrimCommand.Subscribe(ResetTrimming);

            AddChapterCommand.Subscribe(AddChapter);
            PrevChapterCommand.Subscribe(PrevChapter);
            NextChapterCommand.Subscribe(NextChapter);

            ChapterEditing.Subscribe((c) => {
                if (c) {
                    EditingChapterList.Value = Chapters.Value.Values;
                    PanelPosition.Value = player.PanelPosition.RIGHT;
                } else {
                    EditingChapterList.Value = null;
                    SaveChapterListIfNeeds();
                }
            });

            NotifyRange.Subscribe(AddDisabledChapterRange);
            NotifyPosition.Subscribe(AddChapter);


            string prevId = null;
            ReachRangeEnd.Subscribe((prev) => {
                if (prevId == prev) {
                    LoggerEx.error("Next more than twice.");
                }
                if (PlayList.HasNext.Value) {
                    GoForwardCommand.Execute();
                } else {
                    PauseCommand.Execute();
                }
            });

            PanelHorzAlign = PanelPosition.CombineLatest(ChapterEditing, (pos, ed) => {
                if (!ed) {
                    return HorizontalAlignment.Right;
                } else {
                    switch (pos) {
                        case player.PanelPosition.RIGHT: return HorizontalAlignment.Right;
                        case player.PanelPosition.LEFT: return HorizontalAlignment.Left;
                        default: return HorizontalAlignment.Stretch;
                    }
                }
            }).ToReadOnlyReactivePropertySlim();
            PanelVertAlign = PanelPosition.CombineLatest(ChapterEditing, (pos, ed) => {
                if (!ed) {
                    return VerticalAlignment.Bottom;
                }
                else {
                    switch (pos) {
                        case player.PanelPosition.TOP: return VerticalAlignment.Top;
                        case player.PanelPosition.BOTTOM: return VerticalAlignment.Bottom;
                        default: return VerticalAlignment.Stretch;
                    }
                }
            }).ToReadOnlyReactivePropertySlim();

            PanelWidth = PanelPosition.Select(pos => {
                switch (pos) {
                    case player.PanelPosition.RIGHT:
                    case player.PanelPosition.LEFT:
                        return DEF_PANEL_WIDTH;
                    default: return double.NaN;
                }
            }).ToReadOnlyReactivePropertySlim();

            PanelPositionCommand.Subscribe((pos) => {
                PanelPosition.Value = misc.Utils.ParseToEnum(pos, player.PanelPosition.RIGHT);
            });

            TrimmingToChapterCommand.Subscribe(() => {
                var item = PlayList.Current.Value;
                if (item == null) return;
                if (item.TrimStart > 0) {
                    AddDisabledChapterRange(new PlayRange(0, item.TrimStart));
                    ResetTrimming(SetTrimmingStart);
                }
                if (item.TrimEnd > 0) {
                    AddDisabledChapterRange(new PlayRange(item.TrimEnd, 0));
                    ResetTrimming(SetTrimmingEnd);
                }
            });
            if (CheckMode) {
                CheckedCommand.Subscribe(() => {
                    PlayList.Current.Value.Checked = true;
                    GoForwardCommand.Execute();
                });
            }
            CommandManager = new PlayerCommands(this);
        }

        public override void Dispose() {
            base.Dispose();
        }

        // command handlers

        public void SeekRelative(long delta) {
            var pos = (ulong)Math.Min((long)Duration.Value, Math.Max(0, (long)Position.Value + delta));
            Position.Value = pos;
        }
        public void SetRating(Rating rating) {
            var item = PlayList.Current.Value as PlayItem;
            if (item != null) {
                item.Rating = rating;
            }
        }

        #endregion

        #region Setting Chapter By Keyboard

        private double DeferedChapterPosition = 0;
        private DisposablePool ChapterSettingDisposables = new DisposablePool();
        public bool ChapterSettingByKeyboard { get; private set; }

        public void BeginChapterSetting() {
            var current = PlayList.Current.Value;
            if (current == null) {
                CancelChapterSetting();
                return;
            }
            CancelChapterSetting();
            ChapterSettingByKeyboard = true;
            DeferedChapterPosition = (double)Position.Value;
            ChapterSettingDisposables.Add(Position.Subscribe(UpdateChapterSetting));
            ChapterSettingDisposables.Add(PlayList.Current.Subscribe(item => {
                if (current != item) {
                    CancelChapterSetting();
                }
            }));
        }

        private void UpdateChapterSetting(ulong position) {
            if (!ChapterSettingByKeyboard) {
                return;
            }
            if (PlayList.Current.Value == null) {
                CancelChapterSetting();
                return;
            }
            DraggingRange.Value = new PlayRange((ulong)DeferedChapterPosition, position);
        }

        public void CancelChapterSetting() {
            ChapterSettingByKeyboard = false;
            ChapterSettingDisposables.Dispose();
            DraggingRange.Value = null;
        }

        public void CommitChapterSetting() {
            if (!ChapterSettingByKeyboard) {
                return;
            }
            if (PlayList.Current.Value == null) {
                CancelChapterSetting();
                return;
            }
            var endPos = (double)Position.Value;
            if (Math.Abs(endPos - DeferedChapterPosition) > 1000) {
                NotifyRange.Execute(new PlayRange((ulong)DeferedChapterPosition, (ulong)endPos));
            } else {
                NotifyPosition.Execute((ulong)DeferedChapterPosition);
            }
            CancelChapterSetting();
        }
        #endregion
    }
}
