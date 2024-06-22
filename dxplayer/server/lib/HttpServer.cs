// Copyright (C) 2016 by David Jeske, Barend Erasmus and donated to the public domain

using io.github.toyota32k.server.model;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace io.github.toyota32k.server {

    public class HttpServer : IDisposable {
        #region Fields

        private HttpProcessor Processor;
        private Looper ServerLooper = null;

        #endregion

        private static readonly LoggerEx log = new LoggerEx("HttpServer");
        //private bool Alive = true;

        #region Public Methods
        public HttpServer(List<Route> routes) {
            this.Processor = new HttpProcessor();

            foreach (var route in routes) {
                this.Processor.AddRoute(route);
            }
        }

        class Looper: IDisposable {
            private TcpListener Listener = null;
            private bool Alive = true;

            public void Dispose() {
                Stop();
            }

            public bool Start(int port, HttpProcessor processor) {
                try {
                    this.Listener = new TcpListener(IPAddress.Any, port);
                    this.Listener.Start();
                }
                catch (Exception e) {
                    log.error(e);
                    Stop();
                    return false;
                }
                Task.Run(async () => {
                    while (Alive) {
                        try {
                            TcpClient s = await this.Listener.AcceptTcpClientAsync();
                            processor.HandleClient(s);
                        }
                        catch (Exception e) {
                            if (Alive) {
                                log.error(e);
                            }
                        }
                    }
                    lock (this) {
                        Listener?.Stop();
                        Listener = null;
                    }
                });
                return true;
            }
            public void Stop() {
                Alive = false;
                lock (this) {
                    Listener?.Stop();
                    Listener = null;
                }
            }
        }

        public bool Start(int port) {
            Stop();
            var looper = new Looper();
            if (looper.Start(port, Processor)) {
                ServerLooper = looper;
                return true;
            }
            return false;
        }

        public void Stop() {
            ServerLooper?.Stop();
            ServerLooper = null;
        }

        public void Dispose() {
            Stop();
        }
        #endregion
    }
}



