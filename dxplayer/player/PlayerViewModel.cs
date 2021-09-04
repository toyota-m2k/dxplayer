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
using System.Windows.Threading;

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
        public ReactiveProperty<PanelPosition> PanelPosition { get; } = new ReactiveProperty<PanelPosition>(player.PanelPosition.RIGHT);
        public ReadOnlyReactiveProperty<HorizontalAlignment> PanelHorzAlign { get; }
        public ReadOnlyReactiveProperty<VerticalAlignment> PanelVertAlign { get; }
        public ReadOnlyReactiveProperty<double> PanelWidth { get; }
        #endregion

        #region Properties of Item Entry

        public ReactiveProperty<ulong> Duration { get; } = new ReactiveProperty<ulong>(1000);
        public ReactiveProperty<ulong> Position { get; } = new ReactiveProperty<ulong>(0);
        public ReactiveProperty<PlayerState> State { get; } = new ReactiveProperty<PlayerState>(PlayerState.UNAVAILABLE);

        public ReadOnlyReactiveProperty<bool> IsReady { get; }
        public ReadOnlyReactiveProperty<bool> IsPlaying { get; }

        public ReactiveProperty<double> Speed { get; } = new ReactiveProperty<double>(0.5);
        public ReactiveProperty<double> Volume { get; } = new ReactiveProperty<double>(0.5);
        public ReactiveProperty<bool> Mute { get; } = new ReactiveProperty<bool>(false);

        #endregion

        #region Trimming/Chapters
        public ChapterEditor ChapterEditor { get; } = new ChapterEditor();
        //public ReactiveProperty<PlayRange> Trimming { get; } = new ReactiveProperty<PlayRange>(PlayRange.Empty);
        //public ReactiveProperty<ChapterList> Chapters { get; } = new ReactiveProperty<ChapterList>(null, ReactivePropertyMode.RaiseLatestValueOnSubscribe);
        //public ReactiveProperty<List<PlayRange>> DisabledRanges { get; } = new ReactiveProperty<List<PlayRange>>(initialValue:null);
        public ReadOnlyReactiveProperty<bool> HasDisabledRange { get; }
        public ReadOnlyReactiveProperty<bool> HasTrimming { get; }
        //public ReactiveProperty<bool> ChapterEditing { get; } = new ReactiveProperty<bool>(false);
        //public ReactiveProperty<ObservableCollection<ChapterInfo>> EditingChapterList { get; } = new ReactiveProperty<ObservableCollection<ChapterInfo>>();
        public Subject<string> ReachRangeEnd { get; } = new Subject<string>();

        public ReactiveCommand<ulong> NotifyPosition { get; } = new ReactiveCommand<ulong>();
        public ReactiveCommand<PlayRange> NotifyRange { get; } = new ReactiveCommand<PlayRange>();
        public ReactiveProperty<PlayRange?> DraggingRange { get; } = new ReactiveProperty<PlayRange?>(initialValue:null);

        /**
         * 現在再生中の動画のチャプター設定が変更されていればDBに保存する。
         */
        //public void SaveChapterListIfNeeds() {
        //    var storage = App.Instance.DB;
        //    if (null == storage) return;
        //    var item = PlayList.Current.Value;
        //    if (item == null) return;
        //    var chapterList = ChapterEditor.Chapters.Value;
        //    if (chapterList == null || !chapterList.IsModified) return;
        //    storage.ChapterTable.UpdateByChapterList(chapterList);
        //}

        /**
         * 再生する動画のチャプターリスト、トリミング情報、無効化範囲リストを準備する。
         */
        //public void PrepareChapterListForCurrentItem() {
        //    //Chapters.Value = null;
        //    //DisabledRanges.Value = null;
        //    //Trimming.Value = null;
        //    var item = PlayList.Current.Value;
        //    if (item == null) return;
        //    Trimming.Value = new PlayRange(item.TrimStart, item.TrimEnd);
        //    var chapterList = App.Instance.DB.ChapterTable.GetChapterList(item.ID);
        //    if (chapterList != null) {
        //        Chapters.Value = chapterList;
        //        DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
        //    } else {
        //        DisabledRanges.Value = new List<PlayRange>();
        //    }
        //    if (ChapterEditing.Value) {
        //        EditingChapterList.Value = Chapters.Value.Values;
        //    }
        //}

        private void SetTrimming(object obj) {
            switch (obj as String) {
                case "Start": SetTrimmingStartAtCurrentPos(); break;
                case "End": SetTrimmingEndAtCurrentPos(); break;
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

        //private void ResetTrimming(Func<IPlayItem, ulong, PlayRange?> setFunc) {
        //    var item = PlayList.Current.Value;
        //    if (item == null) return;
        //    var trimming = setFunc(item, 0);
        //    if (trimming == null) return;

        //    Trimming.Value = trimming.Value;
        //    DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming.Value).ToList();
        //}

        //private void SetTrimming(Func<IPlayItem, ulong, PlayRange?> setFunc) {
        //    var item = PlayList.Current.Value;
        //    if (item == null) return;
        //    var pos = PlayerPosition;
        //    var trimming = setFunc(item, pos);
        //    if (trimming == null) return;

        //    Trimming.Value = trimming.Value;
        //    DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming.Value).ToList();
        //}

        //--------------------------------------------
        public void SetTrimmingStartAtCurrentPos() {
            ChapterEditor.SetTrimmingStart(PlayerPosition);
        }
        public void SetTrimmingEndAtCurrentPos() {
            ChapterEditor.SetTrimmingEnd(PlayerPosition);
        }
        //--------------------------------------------
        //public void SetTrimmingStart(ulong pos) {
        //    SetTrimmingStart(PlayList.Current.Value, pos);
        //}
        //public void SetTrimmingStart(IPlayItem item, ulong pos) {
        //    if (item == null) return;
        //    var trimming = Trimming.Value;
        //    if (trimming.TrySetStart(pos)) {
        //        item.TrimStart = pos;
        //        Trimming.Value = trimming;
        //        DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming).ToList();
        //    }
        //}

        //public void SetTrimmingEnd(ulong pos) {
        //    SetTrimmingEnd(PlayList.Current.Value, pos);
        //}

        //public void SetTrimmingEnd(IPlayItem item, ulong pos) {
        //    if (item == null) return;
        //    var trimming = Trimming.Value;
        //    if (trimming.TrySetEnd(pos)) {
        //        item.TrimEnd = pos;
        //        Trimming.Value = trimming;
        //        DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming).ToList();
        //    }
        //}

        //--------------------------------------------
        public void ResetTrimmingStart() {
            ChapterEditor.ResetTrimmingStart();
        }
        //public void ResetTrimmingStart(IPlayItem item) {
        //    SetTrimmingStart(item, 0);
        //}
        public void ResetTrimmingEnd() {
            ChapterEditor.ResetTrimmingEnd();
        }
        //public void ResetTrimmingEnd(IPlayItem item) {
        //    SetTrimmingEnd(item, 0);
        //}
        //--------------------------------------------



        //public void NotifyChapterUpdated() {
        //    Chapters.Value.Apply((chapterList) => {
        //        Chapters.Value = chapterList;
        //        DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
        //    });
        //}

        public void AddChapter() {
            ChapterEditor.AddChapter(new ChapterInfo(ChapterEditor, PlayerPosition));
        }

        //private void AddChapter(ulong pos) {
        //    if (PlayList.Current.Value == null) return;
        //    var chapterList = Chapters.Value;
        //    if (chapterList == null) return;
        //    if (pos > Duration.Value) return;
        //    if (chapterList.AddChapter(new ChapterInfo(pos))) {
        //        Chapters.Value = chapterList;
        //        DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
        //    }
        //}

        //private void AddDisabledChapterRange(PlayRange range) {
        //    if (PlayList.Current.Value == null) return;
        //    var chapterList = Chapters.Value;
        //    if (chapterList == null) return;

        //    range.AdjustTrueEnd(Duration.Value);
        //    var del = chapterList.Values.Where(c => range.Start <= c.Position && c.Position <= range.End).ToList();
        //    foreach (var e in del) {    // chapterList.Valuesは ObservableCollection なので、RemoveAll的なやつ使えない。
        //        chapterList.Values.Remove(e);
        //    }
        //    chapterList.AddChapter(new ChapterInfo(range.Start) { Skip = true });
        //    if (range.End != Duration.Value) {
        //        chapterList.AddChapter(new ChapterInfo(range.End));
        //    }
        //    Chapters.Value = chapterList;
        //    DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
        //}

        private void NextChapter() {
            var chapterList = ChapterEditor.Chapters.Value;
            chapterList.GetNeighbourChapterIndex(PlayerPosition, out var prev, out var next);
            if (next >= 0) {
                var c = chapterList.Values[next].Position;
                if (ChapterEditor.Trimming.Value.Contains(c)) {
                    Position.Value = c;
                    return;
                }
            }
            GoForwardCommand.Execute();
        }

        private void PrevChapter() {
            var chapterList = ChapterEditor.Chapters.Value;
            var basePosition = PlayerPosition;
            if (basePosition > 1000)
                basePosition -= 1000;
            chapterList.GetNeighbourChapterIndex(basePosition, out var prev, out var next);
            if (prev >= 0) {
                var c = chapterList.Values[prev].Position;
                if (ChapterEditor.Trimming.Value.Contains(c)) {
                    Position.Value = c;
                    return;
                }
            }
            Position.Value = ChapterEditor.Trimming.Value.Start;
        }

        #endregion

        #region Display Text

        public ReadOnlyReactiveProperty<string> TrimStartText { get; }
        public ReadOnlyReactiveProperty<string> TrimEndText { get; }
        public ReadOnlyReactiveProperty<string> DurationText { get; }
        public ReadOnlyReactiveProperty<string> PositionText { get; }

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
        public ReactiveProperty<bool> FitMode { get; } = new ReactiveProperty<bool>();
        public ReactiveProperty<bool> Fullscreen { get; } = new ReactiveProperty<bool>(false);

        public ReactiveProperty<bool> ReqShowControlPanel { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> ReqShowSizePanel { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> PinControlPanel { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> ShowLabelPanel { get; } = new ReactiveProperty<bool>(false);

        public ReadOnlyReactiveProperty<bool> ShowControlPanel { get; }
        public ReadOnlyReactiveProperty<bool> ShowSizePanel { get; }
        public ReadOnlyReactiveProperty<bool> MinimumPanel { get; }
        public ReadOnlyReactiveProperty<bool> CursorManagerActivity { get; }

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
            DurationText = Duration.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
            PositionText = Position.Select((v) => FormatDuration(v)).ToReadOnlyReactiveProperty();
            TrimStartText = ChapterEditor.Trimming.Select((v) => FormatDuration(v.Start)).ToReadOnlyReactiveProperty();
            TrimEndText = ChapterEditor.Trimming.Select((v) => FormatDuration(v.End)).ToReadOnlyReactiveProperty();
            HasDisabledRange = ChapterEditor.DisabledRanges.Select((c) => c != null && c.Count > 0).ToReadOnlyReactiveProperty();
            HasTrimming = ChapterEditor.Trimming.Select(c => c.Start > 0 || c.End > 0).ToReadOnlyReactiveProperty();
            IsPlaying = State.Select((v) => v == PlayerState.PLAYING).ToReadOnlyReactiveProperty();
            IsReady = State.Select((v) => v == PlayerState.READY || v == PlayerState.PLAYING).ToReadOnlyReactiveProperty();

            ShowSizePanel = ReqShowSizePanel.ToReadOnlyReactiveProperty();
            ShowControlPanel = ReqShowControlPanel.CombineLatest(PinControlPanel, ChapterEditor.IsEditing, (req, pin, editing) => {
                return CheckMode || req || pin || editing;
            }).ToReadOnlyReactiveProperty();
            MinimumPanel = PinControlPanel.CombineLatest(IsPlaying, ChapterEditor.IsEditing, ReqShowControlPanel, (pin, playing, editing, req) => {
                return pin && playing && !CheckMode && !req && !editing;
            }).ToReadOnlyReactiveProperty();
            CursorManagerActivity = ShowControlPanel.CombineLatest(ShowSizePanel, (c, s) => !c && !s).ToReadOnlyReactiveProperty();

            GoForwardCommand.Subscribe(() => {
                ChapterEditor.SaveChapterListIfNeeds();
                PlayList.Next();
            });
            GoBackCommand.Subscribe(() => {
                ChapterEditor.SaveChapterListIfNeeds();
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

            //ChapterEditor.IsEditing.Subscribe((c) => {
            //    if (c) {
            //        PanelPosition.Value = player.PanelPosition.RIGHT;
            //    }
            //});

            NotifyRange.Subscribe((range)=>ChapterEditor.AddDisabledChapterRange(Duration.Value, range));
            NotifyPosition.Subscribe(pos=>ChapterEditor.AddChapter(new ChapterInfo(ChapterEditor, pos)));


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

            PanelHorzAlign = PanelPosition.CombineLatest(ChapterEditor.IsEditing, (pos, ed) => {
                if (!ed) {
                    return HorizontalAlignment.Right;
                } else {
                    switch (pos) {
                        case player.PanelPosition.RIGHT: return HorizontalAlignment.Right;
                        case player.PanelPosition.LEFT: return HorizontalAlignment.Left;
                        default: return HorizontalAlignment.Stretch;
                    }
                }
            }).ToReadOnlyReactiveProperty();
            PanelVertAlign = PanelPosition.CombineLatest(ChapterEditor.IsEditing, (pos, ed) => {
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
            }).ToReadOnlyReactiveProperty();

            PanelWidth = PanelPosition.Select(pos => {
                switch (pos) {
                    case player.PanelPosition.RIGHT:
                    case player.PanelPosition.LEFT:
                        return DEF_PANEL_WIDTH;
                    default: return double.NaN;
                }
            }).ToReadOnlyReactiveProperty();

            PanelPositionCommand.Subscribe((pos) => {
                PanelPosition.Value = misc.Utils.ParseToEnum(pos, player.PanelPosition.RIGHT);
            });

            TrimmingToChapterCommand.Subscribe(() => ChapterEditor.TrimmingToChapter(Duration.Value));
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
        public DisposablePool ChapterSettingDisposables { get; } = new DisposablePool();
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

        #region Super High Speed Mode

        private DispatcherTimer mSuperHighSpeedPlayTimer = null;
        public DisposablePool SuperSpeedModeDisposables { get; } = new DisposablePool();
        public bool IsSuperHighSpeedMode => mSuperHighSpeedPlayTimer != null;
        public void ToggleSuperHighSpeedMode() {
            if(IsSuperHighSpeedMode) {
                ResetSuperHighSpeedMode();
            } else {
                SetSuperHighSpeedMode();
            }
        }

        public void SetSuperHighSpeedMode() {
            if (null == mSuperHighSpeedPlayTimer) {
                Speed.Value = 1;
                mSuperHighSpeedPlayTimer = new DispatcherTimer();
                mSuperHighSpeedPlayTimer.Interval = TimeSpan.FromSeconds(0.1);
                mSuperHighSpeedPlayTimer.Tick += (s, e) => {
                    if (IsPlaying.Value) {
                        SeekRelative(1 * 1000);
                    }
                };
                mSuperHighSpeedPlayTimer.Start();
                SuperSpeedModeDisposables.Add(Speed.ToReadOnlyReactiveProperty(mode:ReactivePropertyMode.DistinctUntilChanged).Subscribe(_ => ResetSuperHighSpeedMode()));
            }
        }

        public void ResetSuperHighSpeedMode() {
            SuperSpeedModeDisposables.Dispose();
            if (mSuperHighSpeedPlayTimer != null) {
                Speed.Value = 0.5;
                mSuperHighSpeedPlayTimer.Stop();
                mSuperHighSpeedPlayTimer = null;
            }
        }

        #endregion
    }
}
