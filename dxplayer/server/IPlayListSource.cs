using dxplayer.data.main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.server {
    public interface IPlayListSource {
        IEnumerable<PlayItem> SelectedItems { get; }
        IEnumerable<PlayItem> ListedItems { get; }
        IEnumerable<PlayItem> AllItems { get; }
        long CurrentId { get; set; }
    }
}
