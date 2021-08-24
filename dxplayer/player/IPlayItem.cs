namespace dxplayer.player {
    public interface IPlayItem {
        ulong TrimStart { get; set; }
        ulong TrimEnd { get; set; }

        string Title { get; }
        string Name { get; }
        bool HasFile { get; }
        string Path { get; }
        bool Checked { get; set; }
        //double Volume { get; }
        //ulong DurationInSec { get; set; }

        void Delete();
    }

    public static class PlayItemExtension {
        public static string TitleOrName(this IPlayItem item) {
            if(!string.IsNullOrWhiteSpace(item.Title)) {
                return item.Title;
            } else if(string.IsNullOrEmpty(item.Name)) {
                return item.Name;
            }
            return "<untitled>";
        }
    }
}
