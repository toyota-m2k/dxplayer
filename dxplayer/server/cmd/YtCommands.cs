using dxplayer.data.main;
using dxplayer.player;
using io.github.toyota32k.server;
using io.github.toyota32k.server.model;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.server.cmd {
    public class YtCommands : ServerCommandListBase {
        enum SourceType {
            DB = 0,
            Listed = 1,
            Selected = 2,
        }

        private static IEnumerable<PlayItem> SourceOf(int type) {
            var source = ServerCommandCenter.Instance.PlayListSource;
            if (source == null) return null;
            switch (type) {
                case (int)SourceType.DB:
                default:
                    return source.AllItems;
                case (int)SourceType.Listed:
                    return source.ListedItems;
                case (int)SourceType.Selected:
                    return source.SelectedItems;
            }
        }

        public Route PlayList { get; } =                     // list: プレイリスト要求
                    new Route {
                        /**
                         * リスト要求
                         * list?c=(category)
                         *      &r=(rating)
                         *      &m=(mark(.mark)*)
                         *      &s=(0:all|1:listed|2:selected)
                         *      &t=(free word)
                         *      &d=(last downloaded time)
                         */
                        Name = "ytPlayer list",
                        UrlRegex = @"/ytplayer/list(?:\?.*)?",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            //Source?.StandardOutput("BooServer: cmd=list");
                            var p = QueryParser.Parse(request.Url);
                            var category = p.GetValue("c");
                            var rating = Convert.ToInt32(p.GetValue("r") ?? "3");
                            var marks = (p.GetValue("m") ?? "0").Split('.').Select((v) => Convert.ToInt32(v)).ToList();
                            var sourceType = Convert.ToInt32(p.GetValue("s") ?? "0");
                            var search = p.GetValue("t");
                            var date = Convert.ToInt64(p.GetValue("d") ?? "0");
                            var current = DateTime.UtcNow.ToFileTimeUtc();
                            var source = SourceOf(sourceType);
                            if (null == source) {
                                return HttpBuilder.ServiceUnavailable();
                            }

                            var list = new JsonArray();
                            if (date == 0) {
                                list.AddRange(
                                    source
                                    .Where((c) => (int)c.Rating >= rating)
                                    .Where((e) => string.IsNullOrEmpty(search) || (e.Name?.ContainsIgnoreCase(search) ?? false) || (e.Desc?.ContainsIgnoreCase(search) ?? false))
                                    .Where((e) => e.Date.ToFileTimeUtc() > date)
                                    .Select((v) => new JsonObject(new Dictionary<string, JsonValue>() {
                                        {"id", v.Path },
                                        {"name", v.TitleOrName() ?? "untitled" },
                                        {"start", $"{v.TrimStart}"},
                                        {"end", $"{v.TrimEnd}" },
                                        {"volume",$"1" }
                                    })));
                            }

                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                {"cmd", "list"},
                                {"date", $"{current}" },
                                {"list",  list}
                            });
                            return new TextHttpResponse(json.ToString(), "application/json");
                        }
                    };

        public Route UpdateCheck { get; } =                     // CHECK: 前回のプレイリストから変更されたかどうかのチェック
                    new Route {
                        Name = "ytPlayer check update",
                        UrlRegex = @"/ytplayer/check(?:\?\w+)?",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            //Source?.StandardOutput($"BooServer: cmd=check");
                            var date = Convert.ToInt64(QueryParser.Parse(request.Url).GetValue("date"));
                            var sb = new StringBuilder();
                            var f = 0; // (date > Storage.LastUpdated) ? 1 : 0;
                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                {"cmd", "check"},
                                {"update", $"{f}" }
                            });
                            return new TextHttpResponse(json.ToString(), "application/json");
                        }
                    };


    }

}
