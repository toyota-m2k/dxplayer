using io.github.toyota32k.server.model;

namespace dxplayer.server {
    public class DxCommands : ServerCommandListBase {

        public Route Root { get; } = new Route {
            Name = "Root",
            UrlRegex = "/dxplayer/",
            Method = "GET",
            Callable = (request) => {
                return new TextHttpResponse(@"DxPlayList Server.", "text/plain");
            }
        };

        // MainCommands
        public Route OpenPlayer { get; } = new Route {
            Name = "Open Player",
            UrlRegex = "/dxplayer/main/openplayer",
            Method = "GET",
            Callable = (request) => {
                ServerCommandCenter.Instance.InvokeMainCommand(cm => {
                    cm[MainCommands.ID.PLAY].Invoke(0);
                });
                return Result("openplayer", true);
            }
        };
    }
}
