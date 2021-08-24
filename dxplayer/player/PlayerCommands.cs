using dxplayer.misc;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace dxplayer.player {
    public class PlayerCommands : KeyCommandManager {
        public enum ID {
            PLAY = 1,
            PAUSE,
            TOGGLE_PLAY,
            MUTE,
            UNMUTE,
            TOGGLE_MUTE,
            CLOSE,

            SEEK_FORWARD_1,
            SEEK_FORWARD_5,
            SEEK_FORWARD_10,
            SEEK_BACK_1,
            SEEK_BACK_5,
            SEEK_BACK_10,

            SLOW_SPEED,
            NORMAL_SPEED,
            DOUBLE_SPEED,
            SUPER_HIGH_SPEED,

            CONTINUOUS_DOUBLE_SPEED,
            CONTINUOUS_SUPER_HIGH_SPEED,

            RATING_EXCELLENT,
            RATING_GOOD,
            RATING_NORMAL,
            RATING_BAD,
            RATING_DREADFUL,

            TOGGLE_STRECH_MODE,
            
            TRIM_SET_START,
            TRIM_SET_END,
            TRIM_RESET_START,
            TRIM_RESET_END,

            PIN_SLIDER,
            KICKOUT_MOUSE,
        }
        public PlayerCommands(PlayerViewModel viewModel) {
            RegisterCommand(
                  CMD(ID.PLAY, "Play", viewModel.PlayCommand)
                , CMD(ID.PAUSE, "Pause", viewModel.PauseCommand)
                , CMD(ID.TOGGLE_PLAY, "TogglePlay", () => (viewModel.IsPlaying.Value ? viewModel.PauseCommand : viewModel.PlayCommand).Execute())
                , CMD(ID.MUTE, "Mute", () => viewModel.Mute.Value = true)
                , CMD(ID.UNMUTE, "Unmute", () => viewModel.Mute.Value = false)
                , CMD(ID.TOGGLE_MUTE, "ToggleMute", () => viewModel.Mute.Value = !viewModel.Mute.Value)
                , CMD(ID.CLOSE, "Close", viewModel.ClosePlayerCommand, "Close Player")
                );
        }

        #region Private

        private static Command CMD(ID id, string name, ReactiveCommand fn, string desc = null) {
            return new Command((int)id, name, desc, fn);
        }
        private static Command CMD(ID id, string name, Action fn, string desc = null) {
            return new Command((int)id, name, desc, fn);
        }
        private void AssignSingleKeyCommand(ID id, Key key) {
            AssignSingleKeyCommand((int)id, key);
        }
        private void AssignControlKeyCommand(ID id, Key key) {
            AssignControlKeyCommand((int)id, key);
        }
        private void AssignShiftKeyCommand(ID id, Key key) {
            AssignShiftKeyCommand((int)id, key);
        }
        private void AssignControlShiftKeyCommand(ID id, Key key) {
            AssignControlShiftKeyCommand((int)id, key);
        }

        #endregion
    }
}
