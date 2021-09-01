using dxplayer.player;
using io.github.toyota32k.toolkit.utils;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dxplayer.data.main {
    public interface IChapterEditUnit {
        bool Do();
        bool Undo();
    }

    public abstract class OwnerdUnitBase {
        private WeakReference<ChapterEditor> mOwner;
        protected ChapterEditor Owner => mOwner?.GetValue();
        public OwnerdUnitBase(ChapterEditor owner) {
            mOwner = new WeakReference<ChapterEditor>(owner);
        }
    }

    class AddRemoveRec : OwnerdUnitBase, IChapterEditUnit {
        bool Add;
        ChapterInfo Target;
        public AddRemoveRec(ChapterEditor owner, ChapterInfo chapter, bool add):base(owner) {
            Add = add;
            Target = chapter;
        }

        public bool Do() {
            if (Add) {
                return Owner.ChapterList.AddChapter(Target);
            }
            else {
                return Owner.ChapterList.RemoveChapter(Target);
            }
        }

        public bool Undo() {
            if (!Add) {
                return Owner.ChapterList.AddChapter(Target);
            }
            else {
                return Owner.ChapterList.RemoveChapter(Target);
            }
        }
    }

    class TrimmingRec : OwnerdUnitBase, IChapterEditUnit {
        private bool Set;
        private bool End;
        private ulong Position;
        public TrimmingRec(ChapterEditor owner, bool set, ulong pos, bool end):base(owner) {
            Set = set;
            Position = pos;
            End = end;
        }

        public bool Do() {
            if (Set) {
                if(!End) {
                    Owner.
                }
            }
            Owner.Trimming.Value = 
        }

        public bool Undo() {
            throw new NotImplementedException();
        }
    }

    abstract class PropRec<T> : IChapterEditUnit {
        T Prev;
        T Next;

        ulong TargetPosition;
        public PropRec(ulong targetPosition, T prev, T next) {
            TargetPosition = targetPosition;
            Prev = prev;
            Next = next;
        }
        protected abstract void SetProp(ChapterInfo chapter, T prop);

        protected ChapterInfo ChapterAt(ChapterList list, ulong position) {
            if (list.GetNeighbourChapterIndex(position, out int prev, out int next)) {
                return list.Values[prev];
            }
            return null;
        }

        public bool Do(ChapterList list) {
            var chapter = ChapterAt(list, TargetPosition);
            if (null == chapter) return false;
            SetProp(chapter, Next);
            return true;
        }

        public bool Undo(ChapterList list) {
            var chapter = ChapterAt(list, TargetPosition);
            if (null == chapter) return false;
            SetProp(chapter, Prev);
            return true;
        }
    }

    class LabelRec : PropRec<string> {
        public LabelRec(ulong targetPosition, string prev, string next) : base(targetPosition, prev, next) {
        }

        protected override void SetProp(ChapterInfo chapter, string prop) {
            chapter.Label = prop;
        }
    }

    class SkipRec : PropRec<bool> {
        public SkipRec(ulong targetPosition, bool prev, bool next) : base(targetPosition, prev, next) {
        }

        protected override void SetProp(ChapterInfo chapter, bool prop) {
            chapter.Skip = prop;
        }
    }

    class CompoundRec : AbsChapterEditHistory, IChapterEditUnit {
        public bool Do(ChapterList list) {
            foreach (var rec in Records) {
                if (!rec.Do(list)) {
                    return false;
                }
            }
            return true;
        }
        public bool Undo(ChapterList list) {
            foreach (var rec in ((IEnumerable<IChapterEditUnit>)Records).Reverse()) {
                if (!rec.Undo(list)) {
                    return false;
                }
            }
            return true;
        }
    }

    public interface IChapterEditHistory {
        bool AddChapter(ChapterList list, ChapterInfo chapter);
        bool RemoveChapter(ChapterList list, ChapterInfo chapter);
        void OnLabelChanged(ChapterInfo chapter, string prev, string current);
        void OnSkipChanged(ChapterInfo chapter, bool current);
    }
    public abstract class AbsChapterEditHistory : IChapterEditHistory {
        protected List<IChapterEditUnit> Records = new List<IChapterEditUnit>(100);

        public virtual bool AddChapter(ChapterInfo chapter) {
            var rec = new AddRemoveRec(chapter, add: true);
            if (rec.Do(list)) {
                Records.Add(rec);
                return true;
            }
            return false;
        }

        public virtual bool RemoveChapter(ChapterList list, ChapterInfo chapter) {
            var rec = new AddRemoveRec(chapter, add: false);
            if (rec.Do(list)) {
                Records.Add(rec);
                return true;
            }
            return false;
        }

        public virtual void OnLabelChanged(ChapterInfo chapter, string prev, string current) {
            var rec = new LabelRec(chapter.Position, prev, current);
            Records.Add(rec);
        }

        public virtual void OnSkipChanged(ChapterInfo chapter, bool current) {
            var rec = new SkipRec(chapter.Position, current, !current);
            Records.Add(rec);
        }

    }

    public class ChapterEditor : AbsChapterEditHistory, IChapterEditUnit {
        public ReactiveProperty<PlayRange> Trimming { get; } = new ReactiveProperty<PlayRange>(PlayRange.Empty);
        public ReactiveProperty<ChapterList> Chapters { get; } = new ReactiveProperty<ChapterList>(null, ReactivePropertyMode.RaiseLatestValueOnSubscribe);
        public ReactiveProperty<List<PlayRange>> DisabledRanges { get; } = new ReactiveProperty<List<PlayRange>>(initialValue: null);
        public ReactiveProperty<bool> ChapterEditing { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<ObservableCollection<ChapterInfo>> EditingChapterList { get; } = new ReactiveProperty<ObservableCollection<ChapterInfo>>();
        public ChapterList ChapterList => Chapters.Value;
        private int CurrentPos = -1;
        
        public ChapterEditor() {
        }

        public void Reset() {
            CurrentPos = -1;
            Records.Clear();
        }

        public void Prepare() {
            if(CurrentPos>=0) {
                Records.RemoveRange(CurrentPos, Records.Count - CurrentPos);
                CurrentPos = -1;
            }
        }

        public void EditInGroup(Action<IChapterEditHistory> fn) {
            var cr = new CompoundRec();
            fn(cr);
            Records.Add(cr);
        }

        public override bool AddChapter(ChapterList list, ChapterInfo chapter) {
            Prepare();
            return base.AddChapter(list, chapter);
        }

        public override bool RemoveChapter(ChapterList list, ChapterInfo chapter) {
            Prepare();
            return base.RemoveChapter(list, chapter);
        }

        public override void OnLabelChanged(ChapterInfo chapter, string prev, string current) {
            Prepare();
            base.OnLabelChanged(chapter, prev, current);
        }

        public override void OnSkipChanged(ChapterInfo chapter, bool current) {
            Prepare();
            base.OnSkipChanged(chapter, current);
        }

        public bool Do(ChapterList list) {
            if(0<=CurrentPos && CurrentPos<Records.Count) {
                if(!Records[CurrentPos].Do(list)) {
                    Reset();
                    return false;
                }
                CurrentPos++;
                if(CurrentPos==Records.Count) {
                    CurrentPos = -1;
                }
                return true;
            }
            return false;
        }

        public bool Undo(ChapterList list) {
            if (CurrentPos < 0) {
                if (Records.Count <= 0) return false;
                CurrentPos = Records.Count;
            }
            if(CurrentPos==0) {
                return false;
            }
            CurrentPos--;
            if(!Records[CurrentPos].Undo(list)) {
                Reset();
                return false;
            }
            return true;
        }

        //--------------------------------------------
        public void SetTrimmingStart(IPlayItem item, ulong pos) {
            if (item == null) return;
            var trimming = Trimming.Value;
            if (trimming.TrySetStart(pos)) {
                item.TrimStart = pos;
                Trimming.Value = trimming;
                DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming).ToList();
            }
        }

        public void SetTrimmingEnd(ulong pos) {
            SetTrimmingEnd(PlayList.Current.Value, pos);
        }

        public void SetTrimmingEnd(IPlayItem item, ulong pos) {
            if (item == null) return;
            var trimming = Trimming.Value;
            if (trimming.TrySetEnd(pos)) {
                item.TrimEnd = pos;
                Trimming.Value = trimming;
                DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming).ToList();
            }
        }

        //--------------------------------------------
        public void ResetTrimmingStart() {
            ResetTrimmingStart(PlayList.Current.Value);
        }
        public void ResetTrimmingStart(IPlayItem item) {
            SetTrimmingStart(item, 0);
        }
        public void ResetTrimmingEnd() {
            ResetTrimmingEnd(PlayList.Current.Value);
        }
        public void ResetTrimmingEnd(IPlayItem item) {
            SetTrimmingEnd(item, 0);
        }
        //--------------------------------------------

    }
}
