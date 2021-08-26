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

    public class HttpServer {
        #region Fields

        private int Port;
        private TcpListener Listener;
        private HttpProcessor Processor;
        //private bool IsActive = true;

        #endregion

        private static readonly LoggerEx log = new LoggerEx("HttpServer");
        private bool Alive = true;

        #region Public Methods
        public HttpServer(int port, List<Route> routes) {
            this.Port = port;
            this.Processor = new HttpProcessor();

            foreach (var route in routes) {
                this.Processor.AddRoute(route);
            }
        }

        //public void Listen()
        //{
        //    this.Listener = new TcpListener(IPAddress.Any, this.Port);
        //    this.Listener.Start();
        //    while (this.IsActive)
        //    {
        //        TcpClient s = this.Listener.AcceptTcpClient();
        //        Thread thread = new Thread(() =>
        //        {
        //            this.Processor.HandleClient(s);
        //        });
        //        thread.Start();
        //        Thread.Sleep(1);
        //    }
        //}

        public bool Start() {
            try {
                this.Listener = new TcpListener(IPAddress.Any, this.Port);
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
                        this.Processor.HandleClient(s);
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
        #endregion
    }
}



