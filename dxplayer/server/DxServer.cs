using dxplayer.data;
using io.github.toyota32k.server;
using io.github.toyota32k.server.model;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dxplayer.server {
    public class DxServer : IDisposable {
        private HttpServer mServer;
        private WeakReference<IStatusBar> mStatusBar;
        private List<Route> Routes { get; set; } = null;

        private IStatusBar StatusBar => mStatusBar.GetValue();
        private bool IsListening { get; set; } = false;

        public DxServer(IStatusBar statusBar) {
            mStatusBar = new WeakReference<IStatusBar>(statusBar);
            InitRoutes();
        }

        public void Start(int port) {
            if (!IsListening) {
                if (null == mServer) {
                    mServer = new HttpServer(Routes);
                }
                if (mServer.Start(port)) {
                    IsListening = true;
                    StatusBar?.OutputStatusMessage($"DxPlayListServer has been started: port={port}");
                }
                else {
                    mServer = null;
                    StatusBar?.OutputStatusMessage($"DxPlayListServer cannot be started: port={port}");
                }
            }
        }

        public void Stop() {
            if (mServer != null) {
                mServer.Stop();
                IsListening = false;
                StatusBar?.OutputStatusMessage($"DxPlayListServer was stopped");
            }
        }

        public void Dispose() {
            Stop();
            mServer = null;
        }

        private void InitRoutes() {
            if (null == Routes) {
                Routes = ServerCommandCenter.Instance.Routes.ToList();
            }
        }
    }
                
}
