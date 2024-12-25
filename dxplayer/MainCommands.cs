using dxplayer.misc;
using dxplayer.server;
using Reactive.Bindings;
using System;
using System.Windows.Input;
using KeyCommandManager = dxplayer.misc.KeyCommandManager;

namespace dxplayer {
    public class MainCommands : KeyCommandManager {

        public enum ID {
            PLAY,
            PLAY_TO_CHECK,
            CHECK,
            UNCHECK,
            DECREMENT_COUNTER,
            SHUTDOWN,
            DELETE_FILES,
            COPY_PATH,
            HELP
        }

        public MainCommands(MainViewModel viewModel) {
            RegisterCommand(
                  CMD(ID.PLAY, "Play", viewModel.PlayCommand, "Open player")
                , CMD(ID.PLAY_TO_CHECK, "PlayToCheck", viewModel.PreviewCommand, "Open player to check")
                , CMD(ID.CHECK, "Check", viewModel.CheckCommand, "Check selected item(s).")
                , CMD(ID.UNCHECK, "Uncheck", viewModel.UncheckCommand, "Uncheck selected item(s).")
                , CMD(ID.DECREMENT_COUNTER, "DecrementCounter", viewModel.DecrementCounterCommand, "Decrement play-counter")
                , CMD(ID.SHUTDOWN, "Shutdown", viewModel.ShutdownCommand)
                , CMD(ID.DELETE_FILES, "DeleteFiles", viewModel.DeleteItemCommand, "Delete selected file(s).")
                , CMD(ID.COPY_PATH, "CopyPath", viewModel.CopyItemPathCommand, "Copy path to the movie file.")
                , CMD(ID.HELP, "Help", viewModel.HelpCommand)
                );

            AssignSingleKeyCommand(ID.PLAY_TO_CHECK,         Key.Enter);       // return == enter
            AssignControlKeyCommand(ID.PLAY,                 Key.G);
            AssignControlKeyCommand(ID.CHECK,                Key.J);
            AssignControlShiftKeyCommand(ID.UNCHECK,         Key.J);
            AssignControlKeyCommand(ID.DECREMENT_COUNTER,    Key.T);
            AssignSingleKeyCommand(ID.DELETE_FILES, Key.Delete);
            AssignSingleKeyCommand(ID.HELP, Key.F1);

            ServerCommandCenter.Instance.Attach(this);
        }

        public override void Dispose() {
            ServerCommandCenter.Instance.Detach(this);
            base.Dispose();
        }

        public Command this[ID id] {
            get => CommandOf((int)id);
        }

        #region Private

        private static Command CMD(ID id, string name, ReactiveCommand fn, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc);
        }
        private static Command CMD(ID id, string name, Action fn, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc);
        }
        private static Command REP_CMD(ID id, string name, ReactiveCommand fn, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetRepeatable(true);
        }
        private static Command REP_CMD(ID id, string name, Action fn, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetRepeatable(true);
        }
        private static Command CNT_CMD(ID id, string name, ReactiveCommand fn, Action brk, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetBreakAction(brk);
        }
        private static Command CNT_CMD(ID id, string name, Action fn, Action brk, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetBreakAction(brk);
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
