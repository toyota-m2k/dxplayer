using System.Collections.Generic;
using System.Linq;

namespace dxplayer.settings
{
    public class MRU {
        private const int MAX_MRU = 10;

        private List<string> list = new List<string>(MAX_MRU + 1);

        public MRU() {
        }
        public MRU(IEnumerable<string>src) {
            list = src.ToList();
        }

        public void AddMru(string path) {
            if (null == path) {
                return;
            }
            list.Remove(path);
            list.Insert(0, path);
            while (list.Count > MAX_MRU) {
                list.RemoveAt(MAX_MRU);
            }
        }

        public bool HasMru => list.Count > 0;

        public IEnumerable<string> List => list;
    }
}
