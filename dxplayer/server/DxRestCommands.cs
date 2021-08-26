using dxplayer.player;
using io.github.toyota32k.server.model;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.server {
    public class DxRestCommands : List<Route> {
        private WeakReference<MainCommands> mMainCommands;
        private WeakReference<PlayerCommands> mPlayerCommands;
        private MainCommands MainCommandManager => mMainCommands?.GetValue();
        private PlayerCommands PlayerCommandManager => mPlayerCommands.GetValue();

        class RouteEx : Route {
            
        }

        public Route Root { get; } = new Route {
            Name = "Root",
            UrlRegex = "/dxplayer/",
            Method = "GET",
            Callable = (request) => {
                return new TextHttpResponse(@"DxPlayList Server.", "text/plain");
            }
        };

        // MainCommands
        public Route OpenPlayer => new Route {
            Name = "Open Player",
            UrlRegex = "/dxplayer/main/openplayer",
            Method = "GET",
            Callable = (request) => {
                MainCommandManager?[MainCommands.ID.PLAY]?.Invoke(0)
            }
        }
    }
}
