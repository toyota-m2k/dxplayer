using dxplayer.player;
using dxplayer.settings;
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

        public long durationRatio = Settings.IsDebug ? 10 : 2;
        public long maxThreshold = Settings.IsDebug ? 5 : 30;

        public PlayCountObserver(PlayerViewModel viewModel) { 
            Add(viewModel.PlayList.Current.Subscribe(OnItemChagned));
            Add(viewModel.IsPlaying.Subscribe(OnPlayingStateChanged));
            Add(viewModel.ChapterEditor.DisabledRanges.Subscribe(OnDisabledRangeChanged));
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
                Playing = playing;
            }
        }

        private long CalcThreshold(ulong duration) {
            return Math.Min((long)duration / durationRatio, maxThreshold * 1000);
        }

        private void OnItemChagned(IPlayItem obj) {
            PlayItem item = obj as PlayItem;
            if(CurrentItem!=item) {
                var tick = System.Environment.TickCount;
                if (Playing) {
                    TickTotal += tick-TickStart;
                    TickStart = tick;
                }
                if(CurrentItem!=null && TickTotal>Threshold) {
                    CurrentItem.PlayCount++;
                    CurrentItem.LastPlayDate = DateTime.UtcNow;
                }
                TickTotal = 0;
                TickStart = tick;
                Threshold = 0;
                if (item != null) {
                    Threshold = CalcThreshold(item.Duration);
                }
                CurrentItem = item;
            }
        }

        private void OnDisabledRangeChanged(List<PlayRange> ranges) {
            if(Utils.IsNullOrEmpty(ranges)) {
                return;
            }
            var duration = CurrentItem.Duration;
            var disabledLength = ranges.Aggregate(0UL, (acc, range) => {
                return acc + range.TrueSpan(duration);
            });
            if(disabledLength<duration) {
                Threshold = CalcThreshold(duration - disabledLength);
            }
        }
    }
}
