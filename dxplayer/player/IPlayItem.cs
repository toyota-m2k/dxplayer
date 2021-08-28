namespace dxplayer.player {
    public interface IPlayItem {
        long ID { get; }
        string Title { get; }
        string Name { get; }
        bool HasFile { get; }
        string Path { get; }
        bool Checked { get; set; }

        ulong TrimStart { get; set; }
        ulong TrimEnd { get; set; }

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
