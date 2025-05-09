using dxplayer.data.main;
using dxplayer.settings;
using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace dxplayer
{
    public enum ListConstraints {
        TitleNonNull,
        TitleDuration,
        SizeDuration,
        DMM,
    }
    public class SweepDuplicationViewModel : ViewModelBase, IListFilter {
        public ReactivePropertySlim<ListConstraints> Criteria { get; } = new ReactivePropertySlim<ListConstraints>(ListConstraints.TitleNonNull);
        public ReactiveCommand SelectAllCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SelectPrevCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SelectNextCommand { get; } = new ReactiveCommand();
        public ReactiveCommand DeleteSelectedCommand { get; } = new ReactiveCommand();

        private IEnumerable<PlayItem> filterByTitleNonNull(IEnumerable<PlayItem> sources) {
            var groups = sources
                 .Where(c => !string.IsNullOrEmpty(c.Title))
                 .GroupBy(c => c.Title)
                 .Where(g => g.Count() > 1);
            var seeker = new Seeker<string>(groups, pointer as Seeker<string>);
            pointer = seeker;
            return seeker.groups.SelectMany(g => g);
        }
        private IEnumerable<PlayItem> filterByTitleDuration(IEnumerable<PlayItem> sources) {
            var groups = sources
                .GroupBy(c => new { c.Title, c.Duration })
                .Where(g => g.Count() > 1);
            var seeker = new Seeker<dynamic>(groups, pointer as Seeker<dynamic>);
            pointer = seeker;
            return seeker.groups.SelectMany(g => g);
        }
        private IEnumerable<PlayItem> filterBySizeDuration(IEnumerable<PlayItem> sources) {
            var groups = sources
                .GroupBy(c => new { c.Size, c.Duration })
                .Where(g => g.Count() > 1);
            var seeker = new Seeker<dynamic>(groups, pointer as Seeker<dynamic>);
            pointer = seeker;
            return seeker.groups.SelectMany(g => g);
        }
        private static readonly Regex regexDmmId = new Regex("(?<id>.*)(4k|4ks|dmb|mhb|hhb|hhbs|_dmb_[sw]|_mhb_[sw]|_hhb_[sw])$");
        private IEnumerable<PlayItem> filterByDMM(IEnumerable<PlayItem> sources) {
            var groups = sources
                .Where(c => !string.IsNullOrEmpty(c.Name) && regexDmmId.IsMatch(c.Name))
                .GroupBy(c => regexDmmId.Match(c.Name).Groups["id"].Value)
                .Where(g => g.Count() > 1);
            var seeker = new Seeker<string>(groups, pointer as Seeker<string>);
            pointer = seeker;
            return seeker.groups.SelectMany(g => g);
        }

        interface ISeeker {
            IEnumerable<PlayItem> Next(PlayItem fromItem=null);
            IEnumerable<PlayItem> Prev(PlayItem fromItem = null);
            void Reset();
            IEnumerable<PlayItem> DeleteCandidateInGroup(IEnumerable<PlayItem> g);
            IEnumerable<PlayItem> DeleteCandidate();
        }
        class Seeker<TKey> : ISeeker {
            int index;
            bool pending = false;
            public List<IGrouping<TKey, PlayItem>> groups;

            public Seeker(IEnumerable<IGrouping<TKey, PlayItem>> groups, Seeker<TKey> old) {
                this.groups = groups.ToList();
                if(old==null) {
                    this.index = -1;
                    return;
                }
                bool tryFindIndex(IGrouping<TKey, PlayItem> entry, out int indexR) {
                    indexR = -1;
                    if (entry == null) return false;
                    indexR = this.groups.FindIndex(it => it.Key.Equals(entry.Key));
                    return indexR >= 0;
                }
                if (tryFindIndex(old.Current, out var cur)) {
                    this.index = cur;
                    pending = true;
                    return;
                }
                if (tryFindIndex(old.peekNext(), out var next)) {
                    this.index = next;
                    pending = true;
                    return;
                }
                if (tryFindIndex(old.peekPrev(), out var prev)) {
                    this.index = prev;
                    pending = true;
                    return;
                }
                this.index = -1;
            }

            private IGrouping<TKey, PlayItem> peekNext() {
                if (index + 1 < groups.Count) {
                    return groups[index + 1];
                }
                else {
                    return null;
                }
            }
            private IGrouping<TKey, PlayItem> peekPrev() {
                if (0 < index) {
                    return groups[index - 1];
                }
                else {
                    return null;
                }
            }
            private IGrouping<TKey, PlayItem> Current {
                get {
                    if (0 <= index && index < groups.Count) {
                        return groups[index];
                    }
                    else {
                        return null;
                    }
                }
            }
            private void seekCurrent(PlayItem fromItem) {
                var i= groups.FindIndex(g => g.Contains(fromItem));
                if(i>=0) {
                    pending = false;
                    index = i;
                }
            }

            public IEnumerable<PlayItem> Next(PlayItem fromItem) {
                if(fromItem!=null) {
                    seekCurrent(fromItem);
                }

                if(pending) {
                    pending = false;
                    return Current;
                }
                if (index+1 < groups.Count) {
                    index++;
                    return groups[index];
                }
                else {
                    index = groups.Count - 1;
                    if(index>=0) {
                        return groups[index];
                    }
                    return null;
                }
            }
            public IEnumerable<PlayItem> Prev(PlayItem fromItem) {
                if (fromItem!=null) { 
                    seekCurrent(fromItem);
                }
                if (pending) {
                    pending = false;
                    return Current;
                }
                if (0 < index) {
                    index--;
                    return groups[index];
                }
                else {
                    if(groups.Count>0) {
                        index = 0;
                        return groups[index];
                    }
                    return null;
                }
            }
            public void Reset() {
                index = -1;
            }

            public IEnumerable<PlayItem> DeleteCandidateInGroup(IEnumerable<PlayItem> g) {
                if(g == null) return null;
                var ex = g.Where(it => it.Checked && !string.IsNullOrEmpty(it.Title)).OrderBy(e => e.Size).FirstOrDefault()
                                ?? g.Where(it => it.Checked).OrderBy(e => e.Size).FirstOrDefault()
                                ?? g.Where(it => !string.IsNullOrEmpty(it.Title)).OrderBy(e => e.Size).FirstOrDefault()
                                ?? g.OrderBy(e => e.Size).FirstOrDefault();
                return g.Where(it => it != ex);
            }

            public IEnumerable<PlayItem> DeleteCandidate() {
                return groups.Select(g => DeleteCandidateInGroup(g))
                    .SelectMany(g => g);
            }
        }
        
        private ISeeker pointer = null;

        public IEnumerable<PlayItem> Filter(IEnumerable<PlayItem> list) {
            switch(Criteria.Value) {
                case ListConstraints.TitleNonNull:
                    return filterByTitleNonNull(list);
                case ListConstraints.TitleDuration:
                    return filterByTitleDuration(list);
                case ListConstraints.SizeDuration:
                    return filterBySizeDuration(list);
                case ListConstraints.DMM:
                    return filterByDMM(list);
                default:
                    return null;
            }
        }

        public IEnumerable<PlayItem> SelectAll() {
            return pointer?.DeleteCandidate();
        }
        public IEnumerable<PlayItem> SelectPrev(PlayItem fromItem=null) {
            return pointer?.DeleteCandidateInGroup(pointer?.Prev(fromItem));
        }
        public IEnumerable<PlayItem> SelectNext(PlayItem fromItem=null) {
            return pointer?.DeleteCandidateInGroup(pointer?.Next(fromItem));
        }
    }
    /// <summary>
    /// SweepDuplicationDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class SweepDuplicationDialog : Window
    {
        public SweepDuplicationViewModel Model => DataContext as SweepDuplicationViewModel;

        public SweepDuplicationDialog()
        {
            DataContext = new SweepDuplicationViewModel();
            InitializeComponent();
        }
        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            Model.Dispose();
        }

        public IListFilter Filter => Model;
    }
}
