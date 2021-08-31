using System.Collections.Generic;

namespace dxplayer.settings {
    public class MRU {
        private const int MAX_MRU = 10;

        public List<string> List { get; set; } = new List<string>(MAX_MRU + 1);

        public MRU() {
        }

        public void AddMru(string path) {
            if (null == path) {
                return;
            }
            List.Remove(path);
            List.Insert(0, path);
            while (List.Count > MAX_MRU) {
                List.RemoveAt(MAX_MRU);
            }
        }

        public bool HasMru => List.Count > 0;
    }
}
