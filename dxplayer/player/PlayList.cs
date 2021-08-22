using Reactive.Bindings;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace dxplayer.player {
    public class PlayList {
        private ReactivePropertySlim<List<IPlayItem>> List { get; } = new ReactivePropertySlim<List<IPlayItem>>(null);
        public ReactivePropertySlim<int> CurrentIndex { get; } = new ReactivePropertySlim<int>(-1, ReactivePropertyMode.RaiseLatestValueOnSubscribe);
        
        public ReadOnlyReactivePropertySlim<int> CurrentPos { get; }
        public ReadOnlyReactivePropertySlim<int> TotalCount { get; }

        public ReadOnlyReactivePropertySlim<IPlayItem> Current { get; }
        public ReadOnlyReactivePropertySlim<bool> HasNext { get; }
        public ReadOnlyReactivePropertySlim<bool> HasPrev { get; }

        public Subject<int> ListItemAdded = new Subject<int>();

        public PlayList() {
            CurrentPos = CurrentIndex.Select((v) => v + 1).ToReadOnlyReactivePropertySlim();
            TotalCount = CurrentIndex.Select((v) => List?.Value?.Count ?? 0).ToReadOnlyReactivePropertySlim();
            Current = List.CombineLatest(CurrentIndex, (list, index) => {
                return (0<=index && index<list.Count) ? list[index] : null;
            }).ToReadOnlyReactivePropertySlim();

            HasNext = List.CombineLatest(CurrentIndex, (list, index) => {
                return index+1 < (list?.Count ?? 0);
            }).ToReadOnlyReactivePropertySlim();
            HasPrev = List.CombineLatest(CurrentIndex, (List, index) => {
                return 0 < index && List!=null;
            }).ToReadOnlyReactivePropertySlim();
        }

        public void SetList(IEnumerable<IPlayItem> s, IPlayItem initialItem =null) {
            List.Value = s.ToList(); //new List<IPlayItem>(s.Where((e) => e.HasFile));
            if(List.Value.Count==0) {
                CurrentIndex.Value = -1;
            } else if(initialItem!=null && List.Value.Contains(initialItem)) { 
                CurrentIndex.Value = List.Value.IndexOf(initialItem);
            } else {
                CurrentIndex.Value = 0;
            }
        }

        public void Add(IPlayItem item) {
            int index = CurrentIndex.Value;
            if (List.Value==null) {
                List.Value = new List<IPlayItem>();
                index = 0;
            }
            if (!List.Value.Where((v) => v.Path == item.Path).Any()) {
                List.Value.Add(item);
            }
            CurrentIndex.Value = index;    // has next を更新するため
            ListItemAdded.OnNext(List.Value.Count-1); // Endedのプレーヤーに再生を再開させるため
        }

        public void Next() {
            if(HasNext.Value) {
                CurrentIndex.Value++;
            }
        }
        public void Prev() {
            if(HasPrev.Value) {
                CurrentIndex.Value--;
            }
        }

        public void DeleteCurrent() {
            var item = Current.Value;
            if(item!=null) {
                item.Delete();
                Next();
            }
        }
    }
}
