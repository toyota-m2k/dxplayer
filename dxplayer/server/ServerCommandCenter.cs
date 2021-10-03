using dxplayer.misc;
using dxplayer.player;
using dxplayer.server.cmd;
using io.github.toyota32k.server.model;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace dxplayer.server {
    public class ServerCommandCenter {
        public static LoggerEx logger = new LoggerEx("ServerCommand");
        private static Lazy<ServerCommandCenter> sInstance = new Lazy<ServerCommandCenter>(() => new ServerCommandCenter());
        public static ServerCommandCenter Instance => sInstance.Value;

        private WeakReference<MainCommands> mMainCommands;
        private WeakReference<PlayerCommands> mPlayerCommands;
        
        private MainCommands MainCommandManager => mMainCommands?.GetValue();
        private PlayerCommands PlayerCommandManager => mPlayerCommands?.GetValue();

        private WeakReference<MainWindow> mMainWindow;
        public MainWindow MainWindow {
            get => mMainWindow.GetValue();
            set { mMainWindow = (value != null) ? new WeakReference<MainWindow>(value) : null; }
        }

        public Dispatcher Dispatcher => MainWindow?.Dispatcher;
        public IPlayListSource PlayListSource => MainWindow;

        private ServerCommandCenter() { }

        public void Attach(KeyCommandManager cm) {
            if(cm is MainCommands) {
                mMainCommands = new WeakReference<MainCommands>(cm as MainCommands);
            } else if(cm is PlayerCommands) {
                mPlayerCommands = new WeakReference<PlayerCommands>(cm as PlayerCommands);
            } else {
                logger.error("Unknown command mamager.");
            }
        }

        public void Detach(KeyCommandManager cm) {
            if(MainCommandManager==cm) {
                mMainCommands = null;
            }
            else if(PlayerCommandManager==cm) {
                mPlayerCommands = null;
            }
        }

        public bool HasPlayer => PlayerCommandManager != null;

        public void InvokeMainCommand(Action<MainCommands> fn) {
            Dispatcher.Invoke(() => {
                var cm = MainCommandManager;
                if (cm != null) {
                    fn(cm);
                }
            });
        }
        public void InvokePlayerCommand(Action<PlayerCommands> fn) {
            Dispatcher.Invoke(() => {
                var cm = PlayerCommandManager;
                if (cm != null) {
                    fn(cm);
                }
            });
        }

        public void RunOnMainThread(Action fn) {
            Dispatcher.Invoke(fn);
        }

        public T RunOnMainThread<T>(Func<T> fn) {
            return Dispatcher.Invoke(fn);
        }

        private DxCommands DxCommands = new DxCommands();
        private WfCommands WfCommands = new WfCommands();
        private YtCommands YtCommands = new YtCommands();

        public IEnumerable<Route> Routes =>
                DxCommands.GetRoutes().Concat(WfCommands.GetRoutes()).Concat(YtCommands.GetRoutes());
    }
}
