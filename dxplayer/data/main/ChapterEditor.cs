using dxplayer.player;
using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
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
        protected virtual ChapterEditor Owner => mOwner?.GetValue();
        public OwnerdUnitBase(ChapterEditor owner) {
            mOwner = new WeakReference<ChapterEditor>(owner);
        }
    }

    class AddRemoveRec : OwnerdUnitBase, IChapterEditUnit {
        bool Add;
        ChapterInfo Target;
        ChapterList ChapterList => Owner?.ChapterList;
        public AddRemoveRec(ChapterEditor owner, ChapterInfo chapter, bool add):base(owner) {
            Add = add;
            Target = chapter;
        }

        private bool Apply(bool add) {
            if (add) {
                return ChapterList?.AddChapter(Target) ?? false;
            }
            else {
                return ChapterList?.RemoveChapter(Target) ?? false;
            }
        }

        public bool Do() {
            return Apply(Add);
        }

        public bool Undo() {
            return Apply(!Add);
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

        private bool Apply(bool set) {
            var owner = Owner;
            if (owner == null) return false;

            if (set) {
                if (!End) {
                    var item = CurrentItem;
                    if (item == null) return;
                    var trimming = Trimming.Value;
                    if (trimming.TrySetStart(pos)) {
                        item.TrimStart = pos;
                        Trimming.Value = trimming;
                        DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming).ToList();
                    }
                } else {
                    owner.SetTrimmingEnd(Position);
                }
            } else {
                if (!End) {
                    owner.ResetTrimmingStart();
                }
                else {
                    owner.ResetTrimmingEnd();
                }
            }
            return true;
        }

        public bool Do() {
            return Apply(Set);
        }

        public bool Undo() {
            return Apply(!Set);
        }
    }

    abstract class PropRec<T> : OwnerdUnitBase, IChapterEditUnit {
        T Prev;
        T Next;

        ulong TargetPosition;
        public PropRec(ChapterEditor owner, ulong targetPosition, T prev, T next):base(owner) {
            TargetPosition = targetPosition;
            Prev = prev;
            Next = next;
        }
        protected abstract void SetProp(ChapterInfo chapter, T prop);

        protected ChapterInfo ChapterAt(ulong position) {
            var list = Owner?.ChapterList;
            if (list!=null && list.GetNeighbourChapterIndex(position, out int prev, out int next)) {
                return list.Values[prev];
            }
            return null;
        }

        public bool Do() {
            var chapter = ChapterAt(TargetPosition);
            if (null == chapter) return false;
            SetProp(chapter, Next);
            return true;
        }

        public bool Undo() {
            var chapter = ChapterAt(TargetPosition);
            if (null == chapter) return false;
            SetProp(chapter, Prev);
            return true;
        }
    }

    class LabelRec : PropRec<string> {
        public LabelRec(ChapterEditor owner, ulong targetPosition, string prev, string next) : base(owner, targetPosition, prev, next) {
        }

        protected override void SetProp(ChapterInfo chapter, string prop) {
            chapter.Label = prop;
        }
    }

    class SkipRec : PropRec<bool> {
        public SkipRec(ChapterEditor owner, ulong targetPosition, bool prev, bool next) : base(owner, targetPosition, prev, next) {
        }

        protected override void SetProp(ChapterInfo chapter, bool prop) {
            chapter.Skip = prop;
        }
    }

    class CompoundRec : AbsChapterEditHistory, IChapterEditUnit {
        public CompoundRec(ChapterEditor owner):base(owner) { }

        public bool Do() {
            foreach (var rec in Records) {
                if (!rec.Do()) {
                    return false;
                }
            }
            return true;
        }
        public bool Undo() {
            foreach (var rec in ((IEnumerable<IChapterEditUnit>)Records).Reverse()) {
                if (!rec.Undo()) {
                    return false;
                }
            }
            return true;
        }
    }

    public interface IChapterEditHistory {
        bool AddChapter(ChapterInfo chapter);
        bool RemoveChapter(ChapterInfo chapter);
        void OnLabelChanged(ChapterInfo chapter, string prev, string current);
        void OnSkipChanged(ChapterInfo chapter, bool current);
        void SetTrimmingStart(ulong pos);
        void SetTrimmingEnd(ulong pos);
        void ResetTrimmingStart();
        void ResetTrimmingEnd();
    }
    public abstract class AbsChapterEditHistory : ViewModelBase<ChapterEditor>, IChapterEditHistory {
        protected List<IChapterEditUnit> Records = new List<IChapterEditUnit>(100);
        public AbsChapterEditHistory(ChapterEditor owner):base(owner) {

        }

        public int CountOfRecords => Records.Count;

        private bool Apply(ChapterInfo chapter, bool add) {
            var owner = Owner;
            if (owner == null) return false;
            var rec = new AddRemoveRec(owner, chapter, add);
            if (rec.Do()) {
                Records.Add(rec);
                return true;
            }
            return false;
        }

        public virtual bool AddChapter(ChapterInfo chapter) {
            return Apply(chapter, true);
        }

        public virtual bool RemoveChapter(ChapterInfo chapter) {
            return Apply(chapter, false);
        }



        public virtual void OnLabelChanged(ChapterInfo chapter, string prev, string current) {
            var owner = Owner;
            if (null == owner) return;
            var rec = new LabelRec(owner, chapter.Position, prev, current);
            Records.Add(rec);
        }

        public virtual void OnSkipChanged(ChapterInfo chapter, bool current) {
            var owner = Owner;
            if (null == owner) return;
            var rec = new SkipRec(owner, chapter.Position, current, !current);
            Records.Add(rec);
        }

        public virtual void SetTrimmingStart(ulong pos) {
            var owner = Owner;
            if (null == owner) return;
            var rec = new TrimmingRec(owner, set: true, pos, end: false);
            if (rec.Do()) {
                Records.Add(rec);
            }
        }
        void SetTrimmingEnd(ulong pos) {
            var owner = Owner;
            if (null == owner) return;
            var rec = new TrimmingRec(owner, set: true, pos, end: true);
            if (rec.Do()) {
                Records.Add(rec);
            }
        }
        void ResetTrimmingStart() {

        }
        void ResetTrimmingEnd();

    }

    public class ChapterEditor : AbsChapterEditHistory, IChapterEditUnit {
        public ReactiveProperty<PlayRange> Trimming { get; } = new ReactiveProperty<PlayRange>(PlayRange.Empty);
        public ReactiveProperty<ChapterList> Chapters { get; } = new ReactiveProperty<ChapterList>(null, ReactivePropertyMode.RaiseLatestValueOnSubscribe);
        public ReactiveProperty<List<PlayRange>> DisabledRanges { get; } = new ReactiveProperty<List<PlayRange>>(initialValue: null);
        public ReactiveProperty<bool> IsEditing { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<ObservableCollection<ChapterInfo>> EditingChapterList { get; } = new ReactiveProperty<ObservableCollection<ChapterInfo>>();
        
        public ChapterList ChapterList => Chapters.Value;
        public IPlayItem CurrentItem { get; private set; }

        private int UndoPosition = -1;
        //protected override ChapterEditor Owner => this;

        // OnMediaOpened()
        public void OnMediaOpened(IPlayItem item) {
            CurrentItem = item;
            if (item == null) {
                Reset();
                return;
            }
            Trimming.Value = new PlayRange(item.TrimStart, item.TrimEnd);
            var chapterList = App.Instance.DB.ChapterTable.GetChapterList(this, item.ID);
            if (chapterList != null) {
                Chapters.Value = chapterList;
                DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
            }
            else {
                DisabledRanges.Value = new List<PlayRange>();
            }
            if (IsEditing.Value) {
                EditingChapterList.Value = Chapters.Value.Values;
            }

        }

        public void NotifyChapterUpdated() {
            var chapterList = Chapters.Value;
            Chapters.Value = chapterList;       //セットしなおすことで更新する
            DisabledRanges.Value = chapterList.GetDisabledRanges(Trimming.Value).ToList();
        }

        public void AddDisabledChapterRange(ulong duration, PlayRange range) {
            if (CurrentItem == null) return;
            var chapterList = Chapters.Value;
            if (chapterList == null) return;

            range.AdjustTrueEnd(duration);
            var del = chapterList.Values.Where(c => range.Start <= c.Position && c.Position <= range.End).ToList();
            var start = new ChapterInfo(this, range.Start);
            EditInGroup((gr) => {
                foreach (var e in del) {    // chapterList.Valuesは ObservableCollection なので、RemoveAll的なやつ使えない。
                    gr.RemoveChapter(e);
                }
                gr.AddChapter(start);
                if (range.End != duration) {
                    gr.AddChapter(new ChapterInfo(this, range.End));
                }
            });
            start.Skip = true;
            NotifyChapterUpdated();
        }

        public void SaveChapterListIfNeeds() {
            var storage = App.Instance.DB;
            if (null == storage) return;
            var item = CurrentItem;
            if (item == null) return;
            var chapterList = Chapters.Value;
            if (chapterList == null || !chapterList.IsModified) return;
            storage.ChapterTable.UpdateByChapterList(this, chapterList);
        }

        public ChapterEditor():base(null) {
            Owner = this;
            IsEditing.Subscribe((c) => {
                if (c) {
                    EditingChapterList.Value = Chapters.Value.Values;
                }
                else {
                    EditingChapterList.Value = null;
                    SaveChapterListIfNeeds();
                }
            });
        }

        public void Reset() {
            SaveChapterListIfNeeds();
            UndoPosition = -1;
            Records.Clear();
            EditingChapterList.Value = null;
            Chapters.Value = null;
            DisabledRanges.Value = null;
            Trimming.Value = PlayRange.Empty;
        }

        public void Prepare() {
            if(UndoPosition>=0) {
                Records.RemoveRange(UndoPosition, Records.Count - UndoPosition);
                UndoPosition = -1;
            }
        }

        public void EditInGroup(Action<IChapterEditHistory> fn) {
            var cr = new CompoundRec(this);
            fn(cr);
            if (cr.CountOfRecords > 0) {
                Records.Add(cr);
            }
        }

        public override bool AddChapter(ChapterInfo chapter) {
            Prepare();
            return base.AddChapter(chapter);
        }

        public override bool RemoveChapter(ChapterInfo chapter) {
            Prepare();
            return base.RemoveChapter(chapter);
        }

        public override void OnLabelChanged(ChapterInfo chapter, string prev, string current) {
            Prepare();
            base.OnLabelChanged(chapter, prev, current);
        }

        public override void OnSkipChanged(ChapterInfo chapter, bool current) {
            Prepare();
            base.OnSkipChanged(chapter, current);
        }

        //--------------------------------------------
        public void SetTrimmingStart(ulong pos) {
            var item = CurrentItem;
            if (item == null) return;
            var trimming = Trimming.Value;
            if (trimming.TrySetStart(pos)) {
                item.TrimStart = pos;
                Trimming.Value = trimming;
                DisabledRanges.Value = Chapters.Value.GetDisabledRanges(trimming).ToList();
            }
        }

        public void SetTrimmingEnd(ulong pos) {
            var item = CurrentItem;
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
            SetTrimmingStart(CurrentItem);
        }
        public void ResetTrimmingStart(IPlayItem item) {
            SetTrimmingStart(item, 0);
        }
        public void ResetTrimmingEnd() {
            ResetTrimmingEnd(CurrentItem);
        }
        public void ResetTrimmingEnd(IPlayItem item) {
            SetTrimmingEnd(item, 0);
        }
        //--------------------------------------------


        public bool Do() {
            if(0<=UndoPosition && UndoPosition<Records.Count) {
                if(!Records[UndoPosition].Do()) {
                    Reset();
                    return false;
                }
                UndoPosition++;
                if(UndoPosition==Records.Count) {
                    UndoPosition = -1;
                }
                return true;
            }
            return false;
        }

        public bool Undo() {
            if (UndoPosition < 0) {
                if (Records.Count <= 0) return false;
                UndoPosition = Records.Count;
            }
            if(UndoPosition==0) {
                return false;
            }
            UndoPosition--;
            if(!Records[UndoPosition].Undo()) {
                Reset();
                return false;
            }
            return true;
        }

    }
}
