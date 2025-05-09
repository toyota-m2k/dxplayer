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
    public enum SelectionOption {
        SizeDuration,
        Size,
        Duration,
        DMM,
    }
    public class SweepDuplicationViewModel : ViewModelBase, IListFilter {
        public ReactivePropertySlim<ListConstraints> Criteria { get; } = new ReactivePropertySlim<ListConstraints>(ListConstraints.TitleNonNull);
        public ReactivePropertySlim<SelectionOption> Option { get; } = new ReactivePropertySlim<SelectionOption>(SelectionOption.SizeDuration);
        public ReactiveCommand SelectCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CloseCommand { get; } = new ReactiveCommand();

        private IEnumerable<PlayItem> filterByTitleNonNull(IEnumerable<PlayItem> sources) {
            return sources
                .Where(c=>!string.IsNullOrEmpty(c.Title))
                .GroupBy(c => c.Title)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);
        }
        private IEnumerable<PlayItem> filterByTitleDuration(IEnumerable<PlayItem> sources) {
            return sources
                .GroupBy(c => new { c.Title, c.Duration })
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);
        }
        private IEnumerable<PlayItem> filterBySizeDuration(IEnumerable<PlayItem> sources) {
            return sources
                .GroupBy(c => new { c.Size, c.Duration })
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);
        }
        private static readonly Regex regexDmmId = new Regex("(?<id>.*)(4k|4ks|dmb|mhb|hhb|_dmb_[sw]|_mhb_[sw]|_hhb_[sw])$");
        private IEnumerable<PlayItem> filterByDMM(IEnumerable<PlayItem> sources) {
            //var groups = sources.Select(c => c.Name)
            //    .Where(c => !string.IsNullOrEmpty(c) && regexDmmId.IsMatch(c))
            //    .GroupBy(c => regexDmmId.Match(c).Groups["id"].Value)
            //    .ToArray();
            //foreach(var g in groups) {
            //    Debug.WriteLine($"{g.Key}: {g.Count()}");
            //}

            return sources
                .Where(c=> !string.IsNullOrEmpty(c.Name) && regexDmmId.IsMatch(c.Name))
                .GroupBy(c => regexDmmId.Match(c.Name).Groups["id"].Value)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);
        }

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
            Model.CloseCommand.Subscribe(_ => Close());
        }
        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            Model.Dispose();
        }

        public IListFilter Filter => Model;
    }
}
