using dxplayer.data.main;
using dxplayer.misc;
using dxplayer.server;
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

            SPEED_SLOW,
            SPEED_NORMAL,
            SPEED_HIGH,
            SPEED_SUPER_HIGH,
            TOGGLE_SPEED_HIGH,
            TOGGLE_SPEED_SUPER_HIGH,

            CONTINUOUS_HIGH_SPEED,
            CONTINUOUS_SUPER_HIGH_SPEED,

            RATING_EXCELLENT,
            RATING_GOOD,
            RATING_NORMAL,
            RATING_BAD,
            RATING_DREADFUL,
            RATING_BAD_AND_NEXT,
            RATING_DREADFUL_AND_NEXT,

            TOGGLE_STRECH_MODE,
            
            TRIM_SET_START,
            TRIM_SET_END,
            TRIM_RESET_START,
            TRIM_RESET_END,

            CHAPTER_SET_BEGIN,
            CHAPTER_SET_COMMIT,
            CHAPTER_SET_CANCEL,

            TOGGLE_CHAPTER_EDIT_MODE,

            TOGGLE_FULLSCREEN,
            TOGGLE_PIN_SLIDER,
            KICKOUT_MOUSE,

            MOVIE_NEXT,
            MOVIE_PREV,
            CHAPTER_NEXT,
            CHAPTER_PREV,

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
                , REP_CMD(ID.SEEK_FORWARD_1, "SeekForward1", () => viewModel.SeekRelative(1000), null, "Seek forward in 1 sec.")
                , REP_CMD(ID.SEEK_FORWARD_5, "SeekForward5", () => viewModel.SeekRelative(5000), null, "Seek forward in 5 sec.")
                , REP_CMD(ID.SEEK_FORWARD_10, "SeekForward10", () => viewModel.SeekRelative(10000), null, "Seek forward in 10 sec.")
                , REP_CMD(ID.SEEK_BACK_1, "SeekBack1", () => viewModel.SeekRelative(-1000), null, "Seek backward in 1 sec.")
                , REP_CMD(ID.SEEK_BACK_5, "SeekBack5", () => viewModel.SeekRelative(-5000), null, "Seek backward in 5 sec.")
                , REP_CMD(ID.SEEK_BACK_10, "SeekBack10", () => viewModel.SeekRelative(-10000), null, "Seek backward in 10 sec.")
                , CMD(ID.SPEED_SLOW, "SpeedSlow", () => viewModel.Speed.Value = 0, "Slow speed")
                , CMD(ID.SPEED_HIGH, "SpeedHigh", () => viewModel.Speed.Value = 1, "High speed")
                , CMD(ID.SPEED_NORMAL, "SpeedNormal", () => viewModel.Speed.Value = 0.5, "Normal speed")
                , CMD(ID.SPEED_SUPER_HIGH, "SpeedSuperHigh", viewModel.SetSuperHighSpeedMode, "Super high speed")
                , CMD(ID.TOGGLE_SPEED_SUPER_HIGH, "ToggleSpeedSuperHigh", viewModel.ToggleSuperHighSpeedMode, "Toggle super-high/normal speed")
                , CMD(ID.TOGGLE_SPEED_HIGH, "ToggleSpeedHigh", () => { viewModel.Speed.Value = (viewModel.Speed.Value > 0.5) ? 0.5 : 1; }, "Toggle high/normal speed")
                , CNT_CMD(ID.CONTINUOUS_HIGH_SPEED, "ContHighSpeed", () => viewModel.Speed.Value = 1, brk: () => viewModel.Speed.Value = 0.5, "Fast forward")
                , CNT_CMD(ID.CONTINUOUS_SUPER_HIGH_SPEED, "ContSuperHighSpeed", viewModel.SetSuperHighSpeedMode, brk: viewModel.ResetSuperHighSpeedMode, "Super fast forward on key prssed")

                , CMD(ID.RATING_EXCELLENT, "RatingExcellent", () => viewModel.SetRating(Rating.EXCELLENT), "Rating:Excellent")
                , CMD(ID.RATING_GOOD, "RatingGood", () => viewModel.SetRating(Rating.GOOD), "Rating:Good")
                , CMD(ID.RATING_NORMAL, "RatingNormal", () => viewModel.SetRating(Rating.NORMAL), "Rating:Normal")
                , CMD(ID.RATING_BAD, "RatingBad", () => viewModel.SetRating(Rating.BAD), "Rating:Bad")
                , CMD(ID.RATING_DREADFUL, "RatingDreadful", () => viewModel.SetRating(Rating.DREADFUL), "Rating:Dreadful")
                , CMD(ID.RATING_BAD_AND_NEXT, "RatingBadAndNext", () => { viewModel.SetRating(Rating.BAD); viewModel.PlayList.Next(); }, "Rating:Bad and next movie")
                , CMD(ID.RATING_DREADFUL_AND_NEXT, "RatingDreadfulAndNext", () => { viewModel.SetRating(Rating.DREADFUL); ; viewModel.PlayList.Next(); }, "Rating:Dreadful and naxt movie.")

                , CMD(ID.TOGGLE_STRECH_MODE, "ToggleStrechMode", () => viewModel.FitMode.Value = !viewModel.FitMode.Value, "Change fitting mode")
                , CMD(ID.TOGGLE_CHAPTER_EDIT_MODE, "ToggleChapterEditMode", () => viewModel.ChapterEditing.Value = !viewModel.ChapterEditing.Value, "Edit chapters")

                , CMD(ID.TOGGLE_PIN_SLIDER, "TogglePinSlider", () => viewModel.PinControlPanel.Value = !viewModel.PinControlPanel.Value, "Pinning slider panel")
                , CMD(ID.KICKOUT_MOUSE, "KickOutMouse", viewModel.KickOutMouseCommand, "Kick mouse cursor out of Player")

                , CMD(ID.TRIM_SET_START, "SetTrimStart", viewModel.SetTrimmingStartAtCurrentPos, "Trimming to current position from head")
                , CMD(ID.TRIM_SET_END, "SetTrimEnd", viewModel.SetTrimmingEndAtCurrentPos, "Trimming from current position to tail")
                , CMD(ID.TRIM_RESET_START, "ResetTrimStart", viewModel.ResetTrimmingStart, "Reset head trimming.")
                , CMD(ID.TRIM_RESET_END, "ResetTrimEnd", viewModel.ResetTrimmingEnd, "Reset tail trimming.")

                , CMD(ID.CHAPTER_SET_BEGIN, "ChapterSetBegin", viewModel.BeginChapterSetting, "Begin disabled chapter from current position")
                , CMD(ID.CHAPTER_SET_COMMIT, "ChapterSetCommit", viewModel.CommitChapterSetting, "Commit disabled chapter to current position")
                , CMD(ID.CHAPTER_SET_CANCEL, "ChapterSetCommit", viewModel.CancelChapterSetting, "Cancel setting chapter")
                , CMD(ID.TOGGLE_FULLSCREEN, "ToggleFullscreen", ()=>viewModel.Fullscreen.Value = !viewModel.Fullscreen.Value, "Fullscreen Mode")

                , CMD(ID.MOVIE_NEXT, "MovieNext", viewModel.PlayList.Next, "Next movie")
                , CMD(ID.MOVIE_PREV, "MovieNext", viewModel.PlayList.Prev, "Previous movie")
                , CMD(ID.CHAPTER_NEXT, "ChapterNext", viewModel.NextChapterCommand, "Next chapter")
                , CMD(ID.CHAPTER_PREV, "ChapterPrev", viewModel.PrevChapterCommand, "Previous chapter")
                );

            AssignSingleKeyCommand(ID.SEEK_FORWARD_1, Key.F);
            AssignShiftKeyCommand(ID.CONTINUOUS_HIGH_SPEED, Key.F);
            AssignControlShiftKeyCommand(ID.CONTINUOUS_SUPER_HIGH_SPEED, Key.F);

            AssignSingleKeyCommand(ID.SEEK_BACK_1, Key.D);

            AssignSingleKeyCommand(ID.CLOSE, Key.Escape);

            AssignSingleKeyCommand(ID.CHAPTER_SET_BEGIN, Key.U);
            AssignSingleKeyCommand(ID.CHAPTER_SET_COMMIT, Key.I);
            AssignSingleKeyCommand(ID.CHAPTER_SET_CANCEL, Key.M);

            AssignSingleKeyCommand(ID.TOGGLE_PLAY, Key.S);

            AssignSingleKeyCommand(ID.RATING_EXCELLENT, Key.D5);
            AssignSingleKeyCommand(ID.RATING_GOOD, Key.D4);
            AssignSingleKeyCommand(ID.RATING_NORMAL, Key.D3);
            AssignSingleKeyCommand(ID.RATING_BAD, Key.D2);
            AssignSingleKeyCommand(ID.RATING_DREADFUL, Key.D1);

            AssignSingleKeyCommand(ID.TRIM_SET_START, Key.J);
            AssignSingleKeyCommand(ID.TRIM_SET_END, Key.K);
            AssignControlKeyCommand(ID.TRIM_RESET_START, Key.J);
            AssignControlKeyCommand(ID.TRIM_RESET_END, Key.K);

            AssignSingleKeyCommand(ID.TOGGLE_CHAPTER_EDIT_MODE, Key.C);

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

        // ボタン押下毎に呼び出される普通のコマンド
        private static Command CMD(ID id, string name, ReactiveCommand fn, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc);
        }
        private static Command CMD(ID id, string name, Action fn, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc);
        }

        /**
         * ボタン押下中に有効化される継続性コマンド。
         * ボタンを放したときに、brkアクションが呼ばれる。
         */
        private static Command CNT_CMD(ID id, string name, ReactiveCommand fn, Action brk, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetBreakAction(brk).SetBreakAction(brk);
        }
        private static Command CNT_CMD(ID id, string name, Action fn, Action brk, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetBreakAction(brk);
        }


        /**
         * ボタン押下中に繰り返して呼ばれるコマンド。
         * 繰り返し回数が必要なら、Action<int>, ReactiveCommand<int> を使用。
         * ボタンを放したときに、brkアクションが呼ばれる。
         */
        private static Command REP_CMD(ID id, string name, ReactiveCommand fn, Action brk=null, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetRepeatable(true);
        }
        private static Command REP_CMD(ID id, string name, ReactiveCommand<int> fn, Action brk = null, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetRepeatable(true);
        }
        private static Command REP_CMD(ID id, string name, Action fn, Action brk=null, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetRepeatable(true).SetBreakAction(brk);
        }
        private static Command REP_CMD(ID id, string name, Action<int> fn, Action brk = null, string desc = null) {
            return new Command((int)id, name, fn).SetDescription(desc).SetRepeatable(true).SetBreakAction(brk);
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
