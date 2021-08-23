namespace dxplayer.data
{
    public interface IStatusBar {
        void OutputStatusMessage(string msg);
        void FlashStatusMessage(string msg, int duration = 5/*sec*/);
    }
}
