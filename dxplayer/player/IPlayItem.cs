using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
