﻿using io.github.toyota32k.toolkit.utils;
using Reactive.Bindings;
using System;
using System.Windows;
using System.Windows.Threading;

namespace dxplayer.player {
    /**
     * 動画再生時にマウスカーソルを非表示にするための管理クラス
     */
    public class CursorManager {
        private static long WAIT_TIME = 2000;   //3ms
        private Point mPosition;
        private long mCheck = 0;
        private DispatcherTimer mTimer = null;
        private WeakReference<Window> mWin;

        private IReadOnlyReactiveProperty<bool> Activator { get; set; } = null;
        private bool Enabled => Activator?.Value ?? false;

        public IDisposable SetActivator(IReadOnlyReactiveProperty<bool> activator) {
            Activator?.Dispose();
            Activator = activator;
            if(activator==null) {
                return null;
            }
            return Activator.Subscribe(Enable);
        }



        public CursorManager(Window owner) {
            mWin = new WeakReference<Window>(owner);
            mPosition = new Point();
        }
        private System.Windows.Input.Cursor CursorOnWin {
            get => mWin?.GetValue().Cursor;
            set {
                var win = mWin?.GetValue();
                if (null != win) {
                    win.Cursor = value;
                }
            }
        }

        /**
         * 当初プロパティにしていたが、プレーヤーを開いたり閉じたりしているとNPEが出たのでメソッドに変えた。
         * セッターメソッドだと ?.オペレータを使えるので、こういうときはプロパティより使いやすいのだ。
         */
        private void Enable(bool enable) {
            if (enable) {
                Update(new Point(-1,-1));
            }
            else {
                Reset();
            }
        }

        public void Reset() {
            LoggerEx.debug("Show Cursor");
            if (mTimer != null) {
                mTimer.Stop();
                mTimer = null;
            }
            CursorOnWin = System.Windows.Input.Cursors.Arrow;
        }

        public void Update(Point pos) {
            if (!Enabled) {
                return;
            }

            if (mPosition != pos) {
                mPosition = pos;
                mCheck = System.Environment.TickCount;
                LoggerEx.debug("Show Cursor");
                CursorOnWin = System.Windows.Input.Cursors.Arrow;
                if (null == mTimer) {
                    mTimer = new DispatcherTimer();
                    mTimer.Tick += OnTimer;
                    mTimer.Interval = TimeSpan.FromMilliseconds(WAIT_TIME / 3);
                    mTimer.Start();
                }
            }
        }

        private void OnTimer(object sender, EventArgs e) {
            if (null == mTimer) {
                return;
            }
            if (System.Environment.TickCount - mCheck > WAIT_TIME) {
                LoggerEx.debug("Hide Cursor");
                mTimer.Stop();
                mTimer = null;
                CursorOnWin = System.Windows.Input.Cursors.None;
                var win = mWin?.GetValue();
                if(win?.WindowStyle == WindowStyle.None) {
                    KickOutMouse();
                }
            }
        }
        public void KickOutMouse() {
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(0, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
        }
    }
}
