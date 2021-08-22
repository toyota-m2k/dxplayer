using dxplayer.player;
using io.github.toyota32k.toolkit.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace dxplayer.data.main {
    public class PlayCountObserver: DisposablePool {
        private PlayItem CurrentItem = null;
        private bool Playing = false;
        private long Threshold = 0;
        private long TickTotal = 0;
        private long TickStart = 0;

        public PlayCountObserver(PlayerViewModel viewModel) { 
            Add(viewModel.PlayList.Current.Subscribe(OnItemChagned));
            Add(viewModel.IsPlaying.Subscribe(OnPlayingStateChanged));
            Add(viewModel.DisabledRanges.Subscribe(OnDisabledRangeChanged));
        }

        private void OnPlayingStateChanged(bool playing) {
            if(Playing!=playing) {
                var tick = System.Environment.TickCount;
                if (!Playing) {
                    // start
                    TickStart = tick;
                } else {
                    // stop
                    TickTotal += tick - TickStart;
                    TickStart = tick;
                }
            }
        }

        private void OnItemChagned(IPlayItem obj) {
            PlayItem item = obj as PlayItem;
            if (item == null) return;
            if(CurrentItem!=item) {
                var tick = System.Environment.TickCount;
                if (Playing) {
                    TickTotal += tick-TickStart;
                    TickStart = tick;
                }
                if(TickTotal>Threshold) {
                    item.PlayCount++;
                    item.LastPlayDate = DateTime.UtcNow;
                }
                TickTotal = 0;
                TickStart = tick;
                Threshold = Math.Min((long)item.Duration / 2, 30 * 1000);
                CurrentItem = item;
            }
        }

        private void OnDisabledRangeChanged(List<PlayRange> ranges) {
            if(Utils.IsNullOrEmpty(ranges)) {
                return;
            }
            var duration = CurrentItem.Duration;
            var disabledLength = ranges.Aggregate(0UL, (acc, range) => {
                return acc + ((range.End > range.Start) ? range.End - range.Start : duration - range.End);
            });
            if(disabledLength<duration) {
                Threshold = Math.Min((long)(duration-disabledLength) / 2, 30 * 1000);
            }
        }
    }
}
