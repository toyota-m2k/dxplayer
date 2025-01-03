﻿using dxplayer.settings;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Windows.Window;

namespace dxplayer.player {
    /// <summary>
    /// Player.xaml の相互作用ロジック
    /// </summary>
    public partial class Player : UserControl {
        PlayerViewModel ViewModel => DataContext as PlayerViewModel;
        private CursorManager mCursorManager;
        private double mReservePosition = 0;

        public Stretch Stretch {
            get => MediaPlayer.Stretch;
            set => MediaPlayer.Stretch = value;
        }

        public Player() {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            ViewModel.Player = this;
            ViewModel.FitMode.Value = Settings.Instance.FitMode;
            ViewModel.FitMode.Subscribe(FitView);
            ViewModel.MaximizeCommand.Subscribe(ToggleFullscreen);
            ViewModel.Fullscreen.Value = GetWindow(this)?.WindowStyle == WindowStyle.None;
            mCursorManager = new CursorManager(GetWindow(this));
            ViewModel.KickOutMouseCommand.Subscribe(mCursorManager.KickOutMouse);

            ViewModel.Speed.Subscribe((speed) => {
                double sr = (speed >= 0.5) ? 1 + (speed - 0.5) * 2 /* 1 ～ 2 */ : 0.2 + 0.8 * (speed * 2)/*0.2 ～ 1*/;
                MediaPlayer.SpeedRatio = sr;
            });
            ViewModel.Volume.Subscribe((volume) => {
                MediaPlayer.Volume = volume;
            });
            ViewModel.PlayList.Current.Subscribe(OnCurrentItemChanged);

            ViewModel.PlayCommand.Subscribe(Play);
            ViewModel.PauseCommand.Subscribe(Pause);
            ViewModel.ChapterEditor.IsEditing.Subscribe(OnChapterEditing);

            mCursorManager.SetActivator(ViewModel.CursorManagerActivity);
        }

        private void OnCurrentItemChanged(IPlayItem item) {
            ViewModel.EndRepeatSkippingMode();
            ViewModel.State.Value = PlayerState.UNAVAILABLE;
            MediaPlayer.Stop();
            MediaPlayer.Source = null;
            //ViewModel.ChapterEditor.SaveChapterListIfNeeds();
            ViewModel.ChapterEditor.Reset();

            mReservePosition = 0;
            Uri uri = null;
            if (item != null) {
                if (item.Path == Settings.Instance.LastPlayingPath && Settings.Instance.LastPlayingPos > 0) {
                    mReservePosition = Settings.Instance.LastPlayingPos;
                } else {
                    mReservePosition = item.TrimStart;
                }
                //ViewModel.Volume.Value = item.Volume;

                string path = item.Path;
                if(!string.IsNullOrEmpty(path)) {
                    uri = new Uri(path);
                    ViewModel.State.Value = PlayerState.LOADING;
                }
                if (uri != null) {
                    LoggerEx.debug($"New Source.{uri}");
                    MediaPlayer.Source = uri;
                    // Sourceをセットしただけでは OnMediaOpenedが呼ばれない。
                    // Play または、Stop を呼んでおく必要がある。
                    //MediaPlayer.Pause();
                }
            }
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e) {
            LoggerEx.debug("");
            if (!MediaPlayer.NaturalDuration.HasTimeSpan) {
                LoggerEx.debug("no duration");
                return;
            }

            LoggerEx.debug("ok");
            ViewModel.State.Value = PlayerState.READY;
            ViewModel.Duration.Value = (ulong)MediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            var current = ViewModel.PlayList.Current.Value;
            ViewModel.ChapterEditor.OnMediaOpened(current);
//            Play();     // 一旦 Playを呼んでおかないと、シークしてから再生したときに、なぜか先頭に戻ってしまう。
            if (ViewModel.AutoPlay) {
                Play();
                double pos = 0;
                if (mReservePosition > 0 && mReservePosition < ViewModel.Duration.Value) {
                    pos = mReservePosition;
                }
                MediaPlayer.Position = TimeSpan.FromMilliseconds(pos);
                mReservePosition = 0;
            } else {
                Play();     // 一旦 Playを呼んでおかないと、シークしてから再生したときに、なぜか先頭に戻ってしまう。
                Pause();
            }
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e) {
            ViewModel.EndRepeatSkippingMode();
            ViewModel.State.Value = PlayerState.READY;
            ControlPanel.Slider.OnMediaEnd();
            //if (mCurrentItemId != null) {
            //    ViewModel.ReachRangeEnd.OnNext(mCurrentItemId);
            //    mCurrentItemId = null;
            //}
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e) {
            ViewModel.State.Value = PlayerState.ERROR;
            LoggerEx.error(e.ErrorException);

            // エラー表示と、Retry or Next 選択

            //if (mCurrentItemId != null) {
            //    ViewModel.ReachRangeEnd.OnNext(mCurrentItemId);
            //    mCurrentItemId = null;
            //}
        }

        public void Play() {
            if (ViewModel.IsReady.Value) {
                MediaPlayer.Play();
                ViewModel.State.Value = PlayerState.PLAYING;
            }
        }

        public void Pause() {
            if (ViewModel.IsPlaying.Value) {
                MediaPlayer.Pause();
                ViewModel.State.Value = PlayerState.READY;
            }
        }

        public void FitView(bool mode) {
            Stretch = mode ? Stretch.Uniform : Stretch.UniformToFill;
            Settings.Instance.FitMode = mode;
        }

        //public void Stop() {
        //    if (ViewModel.IsReady.Value) {
        //        MediaPlayer.Stop();
        //        ViewModel.State.Value = PlayerState.READY;
        //        ViewModel.Position.Value = 0;
        //    }
        //}

        public double SeekPosition {
            get => ViewModel.IsReady.Value ? MediaPlayer.Position.TotalMilliseconds : 0;
            set {
                if (ViewModel.IsReady.Value) {
                    MediaPlayer.Position = TimeSpan.FromMilliseconds(value);
                } else {
                    LoggerEx.error("cannot seek. (movie is not ready.)");
                }
            }
        }

        private void ShowPanel(FrameworkElement panel, bool show) {
            var vm = ViewModel;
            if (vm == null) return;
            switch (panel?.Tag as string) {
                case "ControlPanel":
                    vm.ReqShowControlPanel.Value = show;
                    break;
                case "SizingPanel":
                    vm.ReqShowSizePanel.Value = show;
                    break;
                default:
                    return;
            }
        }

        private void OnMouseEnter(object sender, MouseEventArgs e) {
            ShowPanel(sender as FrameworkElement, true);
        }

        private void OnMouseMove(object sender, MouseEventArgs e) {
            mCursorManager.Update(e.GetPosition(this));
        }

        private void OnMouseLeave(object sender, MouseEventArgs e) {
            ShowPanel(sender as FrameworkElement, false);
        }

        private void ToggleFullscreen() {
            var win = GetWindow(this);
            if (win == null) return;
            if (win.WindowStyle == WindowStyle.None) {
                win.WindowStyle = WindowStyle.SingleBorderWindow;
                win.WindowState = WindowState.Normal;
                ViewModel.Fullscreen.Value = false;
            }
            else {
                win.WindowStyle = WindowStyle.None; // タイトルバーと境界線を表示しない
                win.WindowState = WindowState.Maximized; // 最大化表示
                ViewModel.Fullscreen.Value = true;
            }
        }

        private void OnPlayerClicked(object sender, MouseButtonEventArgs e) {
            if(ViewModel.IsPlaying.Value) {
                Pause();
            } else {
                Play();
            }
        }

        private void OnChapterEditing(bool edit) {
            if(edit) {
                ViewModel.ReqShowControlPanel.Value = true;
                ViewModel.ReqShowSizePanel.Value = false;
            }
        }
    }
}
