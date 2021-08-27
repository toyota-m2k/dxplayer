﻿using io.github.toyota32k.server.model;
using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.server {
    public abstract class ServerCommandListBase {
        protected static IHttpResponse Result(string cmd, bool result) {
            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                {"cmd", cmd },
                                {"result",  true}
                            });
            return new TextHttpResponse(json.ToString(), "application/json");
        }



        public IEnumerable<Route> GetRoutes() {
            var type = this.GetType();
            var props = type.GetProperties();
            foreach(var p in props) {
                var indexer = p.GetIndexParameters();
                if (indexer.Length == 0) {  // indexerは除外
                    var obj = p.GetValue(this);
                    if (obj is Route) {
                        yield return obj as Route;
                    }
                }
            }
        }
    }
}