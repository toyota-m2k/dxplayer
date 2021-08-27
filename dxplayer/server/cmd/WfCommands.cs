using dxplayer.player;
using io.github.toyota32k.server.model;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace dxplayer.server.cmd {
    public class WfCommands : ServerCommandListBase {
        static class WfName {
            public const string PLAY = "play";
            public const string PAUSE = "pause";
            public const string STOP = "stop";
            public const string FAST_PLAY = "fast";
            public const string MUTE = "mute";
            public const string CLOSE = "close";
            public const string NEXT = "next";
            public const string PREV = "prev";
            public const string SEEK_FWD = "fwd";
            public const string SEEK_BACK = "back";
            public const string SEEK_FWD_L = "fwdL";
            public const string SEEK_BACK_L = "backL";
            public const string SEEK_FWD_10 = "fwd10";
            public const string SEEK_BACK_10 = "back10";
            public const string SEEK_FWD_5 = "fwd5";
            public const string SEEK_BACK_5 = "back5";
            public const string PLAY_SUPER_FAST = "superFast";
            public const string RATING_GOOD = "good";
            public const string RATING_NORMAL = "normal";
            public const string RATING_BAD = "bad";
            public const string RATING_DREADFUL = "dreadful";
            public const string NEXT_STD_STRETCH = "std";
            public const string NEXT_CST_STRETCH = "custNext";
            public const string PREV_CST_STRETCH = "custPrev";

            //public const string TRIM_EDIT = "trimEdit";
            //public const string TRIM_SELECT = "trimSelect";
            public const string TRIM_SET_START = "trimSetStart";
            public const string TRIM_SET_END = "trimSetEnd";
            public const string TRIM_RESET_START = "trimResetStart";
            public const string TRIM_RESET_END = "trimResetEnd";

            public const string PIN_SLIDER = "showSlider";

            public const string KICKOUT_MOUSE = "kickOutMouse";
            public const string SHUTDOWN = "shutdown";
        }


        static Dictionary<string, PlayerCommands.ID> KeyMap = new Dictionary<string, PlayerCommands.ID>() {
            { "play", PlayerCommands.ID.PLAY },
            { "pause", PlayerCommands.ID.PAUSE },
            { "stop", PlayerCommands.ID.PAUSE },
            { "fast", PlayerCommands.ID.TOGGLE_SPEED_HIGH },
            { "mute", PlayerCommands.ID.TOGGLE_MUTE },
            { "close", PlayerCommands.ID.CLOSE },
            { "next", PlayerCommands.ID.MOVIE_NEXT },
            { "prev", PlayerCommands.ID.MOVIE_PREV },
            { "fwd", PlayerCommands.ID.SEEK_FORWARD_1 },
            { "back", PlayerCommands.ID.SEEK_BACK_1 },
            { "fwdL", PlayerCommands.ID.SEEK_FORWARD_5 },
            { "backL", PlayerCommands.ID.SEEK_BACK_5 },
            { "fwd10", PlayerCommands.ID.SEEK_FORWARD_10 },
            { "back10", PlayerCommands.ID.SEEK_BACK_10 },
            { "fwd5", PlayerCommands.ID.SEEK_FORWARD_5 },
            { "back5", PlayerCommands.ID.SEEK_BACK_5 },
            { "superFast", PlayerCommands.ID.TOGGLE_SPEED_SUPER_HIGH },
            { "good", PlayerCommands.ID.RATING_GOOD },
            { "normal", PlayerCommands.ID.RATING_NORMAL },
            { "bad", PlayerCommands.ID.RATING_BAD_AND_NEXT },
            { "dreadful", PlayerCommands.ID.RATING_DREADFUL_AND_NEXT },
            { "std", PlayerCommands.ID.TOGGLE_STRECH_MODE },
            { "custNext", PlayerCommands.ID.TOGGLE_STRECH_MODE },
            { "custPrev", PlayerCommands.ID.TOGGLE_STRECH_MODE },
            { "trimSetStart", PlayerCommands.ID.TRIM_SET_START },
            { "trimSetEnd", PlayerCommands.ID.TRIM_SET_END },
            { "trimResetStart", PlayerCommands.ID.TRIM_RESET_START },
            { "trimResetEnd", PlayerCommands.ID.TRIM_RESET_END },
            { "showSlider", PlayerCommands.ID.TOGGLE_PIN_SLIDER },
            { "kickOutMouse", PlayerCommands.ID.KICKOUT_MOUSE },
        };

        static Regex mRegex = new Regex(@"/wfplayer/cmd/(?<cmd>[a-zA-Z]+)(/(?<param>\w*))?");

        public Route WF { get; } = new Route {
            Name = "wfPlayer command",
            UrlRegex = @"^/wfplayer/cmd/.*",
            Method = "GET",
            Callable = (HttpRequest request) => {
                var match = mRegex.Match(request.Url);
                var cmd = "unknown";
                var result = false;
                if (match.Success) {
                    cmd = match.Groups["cmd"].Value;
                    if(KeyMap.TryGetValue(cmd, out var id)) {
                        ServerCommandCenter.Instance.InvokePlayerCommand(cm=>cm[id].Invoke(0));
                        result = true;
                    }
                }
                return Result(cmd, result);
            }
        };
    }
}
