using dxplayer.data;
using io.github.toyota32k.server;
using io.github.toyota32k.server.model;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.server {
    public class DxServer : IDisposable {
        private int mPort;
        private HttpServer mServer;
        private WeakReference<IStatusBar> mStatusBar;
        private List<Route> Routes { get; set; } = null;

        private IStatusBar StatusBar => mStatusBar.GetValue();
        private bool IsListening { get; set; } = false;

        public DxServer(IStatusBar statusBar, int port = 5000) {
            mStatusBar = new WeakReference<IStatusBar>(statusBar);
            mPort = port;
            InitRoutes();
        }

        public void Start() {
            if (!IsListening) {
                if (null == mServer) {
                    mServer = new HttpServer(mPort, Routes);
                }
                if (mServer.Start()) {
                    IsListening = true;
                    StatusBar?.OutputStatusMessage($"DxPlayListServer has been started: port={mPort}");
                }
                else {
                    mServer = null;
                    StatusBar?.OutputStatusMessage($"DxPlayListServer cannot be started: port={mPort}");
                }
            }
        }

        public void Stop() {
            if (mServer != null) {
                mServer.Stop();
                IsListening = false;
                StatusBar?.OutputStatusMessage($"DxPlayListServer was stopped: port={mPort}");
            }
        }

        public void Dispose() {
            Stop();
            mServer = null;
        }

        private void InitRoutes() {
            if (null == Routes) {
                Routes = new List<Route> {


                };
            }
        }
    }
                
}
