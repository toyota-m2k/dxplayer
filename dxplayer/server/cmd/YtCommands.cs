using dxplayer.data.main;
using dxplayer.player;
using dxplayer.settings;
using io.github.toyota32k.server;
using io.github.toyota32k.server.model;
using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace dxplayer.server.cmd {
    public class YtCommands : ServerCommandListBase {
        enum SourceType {
            DB = 0,
            Listed = 1,
            Selected = 2,
        }

        private static IPlayListSource Source => ServerCommandCenter.Instance.PlayListSource;
        private static IEnumerable<PlayItem> SourceOf(int type) {
            var source = Source;
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
        private static MainStorage DB { get => App.Instance.DB; }
        public Route Capability { get; }
            = new Route {
                Name = "Capability of this server",
                UrlRegex = "/capability",
                Method = "GET",
                Callable = (request) => {
                    var json = new JsonObject(new Dictionary<string, JsonValue>() {
                        {"cmd", "capability"},
                        {"serverName", "DxPlayer"},
                        {"version", 1},
                        {"root", "/ytplayer/" },
                        {"category", false},
                        {"rating", true},
                        {"mark", false},
                        {"chapter", true},
                        {"reputation", 1 },             // reputation (category/mark/rating) コマンド対応 1:RO /2:RW
                        {"diff", false},                // date以降の更新チェック(check)、差分リスト取得に対応
                        {"sync", false },               // 端末間同期
                        {"acceptRequest", false },        // register command をサポートする
                        {"hasView", true },              // current get/set をサポートする
                        {"authentication", false},
                        {"types", "v" }
                    });
                    return new TextHttpResponse(request, json.ToString(), "application/json");
                }
            };

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
                                        {"id", v.ID },
                                        {"name", v.TitleOrName() ?? "untitled" },
                                        {"start", $"{v.TrimStart}"},
                                        {"end", $"{v.TrimEnd}" },
                                        {"volume",$"1" },
                                        {"media", "v" },
                                        {"duration", v.Duration }
                                    })));
                            }

                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                {"cmd", "list"},
                                {"date", $"{current}" },
                                {"list",  list}
                            });
                            return new TextHttpResponse(request, json.ToString(), "application/json");
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
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    };
        public Route ChapterList { get; } =  // chapter: チャプターリスト要求
                    new Route {
                        Name = "ytPlayer chapters",
                        UrlRegex = @"/ytplayer/chapter\?\w+",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            var id = Convert.ToInt64(QueryParser.Parse(request.Url)["id"]);
                            var chapters = DB.ChapterTable.List.Where(c => c.Owner == id);
                            if (null == chapters) {
                                logger.error($"YT: cmd=chapter ({id}).");
                                return HttpBuilder.ServiceUnavailable();
                            }
                            //Source?.StandardOutput($"BooServer: cmd=chapter ({id})");
                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                            { "cmd", "chapter"},
                                            { "id", $"{id}" },
                                            { "chapters", new JsonArray(
                                                chapters.Select((c) => new JsonObject(new Dictionary<string,JsonValue>(){
                                                        { "position", c.Position },
                                                        { "label", c.Label },
                                                        { "skip", c.Skip }
                                                }))) },
                                            }
                            );
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    };
        // ratings: 全レーティングリストの要求
        public Route RatingList { get; } =
            new Route {
                Name = "ytPlayer Ratings",
                UrlRegex = @"/ytplayer/ratings",
                Method = "GET",
                Callable = request => {
                    //Source?.StandardOutput("BooServer: cmd=rating");
                    Logger.debug("YtServer: Ratings");
                    var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                {"cmd", "ratings"},
                                {"default",  3 },
                                {"ratings", new JsonArray(
                                    Enum.GetValues(typeof(Rating)).ToEnumerable<Rating>().Select((v)=> new JsonObject(new Dictionary<string, JsonValue>() {
                                        {"rating", (int)v},
                                        {"label",v.ToString()},
                                    })).ToArray()) },
                            });
                    return new TextHttpResponse(request, json.ToString(), "application/json");
                }
            };

        private static void UpdatePlayCounterIfNeed(PlayItem item) {
            if(Settings.Instance.PlayCountFromServer) {
                var now = DateTime.UtcNow;
                var prev = item.LastPlayDate;
                var d = now - prev;
                if(d.TotalMinutes < 60) {
                    return;     // 60分以内に複数回再生された場合は1回とみなす。
                }
                item.LastPlayDate = now;
                item.PlayCount++;
                DB.PlayListTable.Update();
            }
        }

        private static Func<HttpRequest, IHttpResponse> RequestStream = (HttpRequest request) => {
            var source = DB.PlayListTable.List;
            if (null == source) {
                return HttpBuilder.ServiceUnavailable();
            }
            var id = Convert.ToInt64(QueryParser.Parse(request.Url)["id"]);
            var entry = source.Where((e) => e.ID == id).SingleOrDefault();
            if (null == entry) {
                return HttpBuilder.NotFound();
            }
            UpdatePlayCounterIfNeed(entry);

            var range = request.Headers.GetValue("Range");
            if (null == range) {
                //Source?.StandardOutput($"BooServer: cmd=video({id})");
                return new StreamingHttpResponse(request, entry.Path, "video/mp4", 0, 0);
            }
            else {
                var m = RegRange.Match(range);
                var s = m.Groups["start"];
                var e = m.Groups["end"];
                var start = s.Success ? Convert.ToInt64(s.Value) : 0;
                var end = e.Success ? Convert.ToInt64(s.Value) : 0;
                return new StreamingHttpResponse(request, entry.Path, "video/mp4", start, end);
            }
        };

        private static Regex RegRange = new Regex(@"bytes=(?<start>\d+)(?:-(?<end>\d+))?");
        public Route MovieStream { get; } = // VIDEO:ビデオストリーム要求
                    new Route {
                        Name = "ytPlayer video",
                        UrlRegex = @"/ytplayer/video\?\w+",
                        Method = "GET",
                        Callable = RequestStream
                    };
        public Route ItemStream { get; } = // VIDEO:ビデオストリーム要求
                    new Route {
                        Name = "ytPlayer video",
                        UrlRegex = @"/ytplayer/item\?\w+",
                        Method = "GET",
                        Callable = RequestStream
                    };
        public Route GetCurrent { get; } = // current: カレントアイテムのget/set
                    new Route {
                        Name = "ytPlayer Current Item",
                        UrlRegex = @"/ytplayer/current",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            //Source?.StandardOutput("BooServer: cmd=current (get)");
                            Logger.debug("YtServer: Current (Get)");
                            var nid = ServerCommandCenter.Instance.RunOnMainThread(()=>Source.CurrentId);
                            var id = nid != 0 ? $"{nid}" : "";
                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                { "cmd", "current"},
                                { "id", id },
                            });
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    };
        public Route PutCurrent { get; } =
                    new Route {
                        Name = "ytPlayer Current Item",
                        UrlRegex = @"/ytplayer/current",
                        Method = "PUT",
                        Callable = (HttpRequest request) => {
                            //Source?.StandardOutput("BooServer: cmd=current (put)");
                            Logger.debug("YtServer: Current (Put)");
                            if (!request.Headers.TryGetValue("Content-Type", out string type)) {
                                return HttpBuilder.BadRequest();
                            }
                            try {
                                var json = new JsonHelper(request.Content);
                                var id = json.GetLong("id");
                                ServerCommandCenter.Instance.RunOnMainThread(() => {
                                    Source.CurrentId = id;
                                });
                                return new TextHttpResponse(request, json.ToString(), "application/json");
                            }
                            catch (Exception e) {
                                Logger.error(e);
                                return HttpBuilder.InternalServerError();
                            }
                        }
                    };
        public Route Categories = // category：全カテゴリリストの要求
                    new Route {
                        Name = "ytPlayer Not Supported",
                        UrlRegex = @"/ytplayer/category",
                        Method = "GET",
                        Callable = (HttpRequest request) => {
                            //Source?.StandardOutput("BooServer: cmd=category");
                            Logger.debug("YtServer: Category");
                            var json = new JsonObject(new Dictionary<string, JsonValue>() {
                                            {"cmd", "category"},
                                            {"categories", new JsonArray() }
                                        });
                            return new TextHttpResponse(request, json.ToString(), "application/json");
                        }
                    };

    }

}
