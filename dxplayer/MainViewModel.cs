using dxplayer.data.main;
using dxplayer.settings;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace dxplayer
{
    public enum PlayingStatus {
        IDLE,
        PLAYING,
        CHECKING,
    }

    public class MainViewModel : ViewModelBase
    {
        #region Commands
        public ReactiveCommand PlayCommand { get; } = new ReactiveCommand();
        public ReactiveCommand PreviewCommand { get; } = new ReactiveCommand();
        public ReactiveCommand DeleteItemCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SettingCommand { get; } = new ReactiveCommand();
        public ReactiveCommand<string> CheckedFilterCommand { get; } = new ReactiveCommand<string>();
        public ReactiveCommand<string> CountFilterCommand { get; } = new ReactiveCommand<string>();

        public ReactiveCommand AddFolderCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ImportFromWfCommand { get; } = new ReactiveCommand();
        public ReactiveCommand RefreshAllCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SelectDupCommand { get; } = new ReactiveCommand();

        public ReactiveCommand CheckCommand { get; } = new ReactiveCommand();
        public ReactiveCommand UncheckCommand { get; } = new ReactiveCommand();
        public ReactiveCommand DecrementCounterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ResetCounterCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ShutdownCommand { get; } = new ReactiveCommand();


        public MainCommands CommandManager { get; }

        public DialogViewModel Dialog { get; } = new DialogViewModel();

        #endregion

        #region Property / Status
        public ReactiveProperty<string> StatusMessage { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<ObservableCollection<PlayItem>> MainList { get; } = new ReactiveProperty<ObservableCollection<PlayItem>>(new ObservableCollection<PlayItem>());
        public ReadOnlyReactiveProperty<int> ItemCount { get; }
        public SortInfo SortInfo => Settings.Instance.SortInfo;
        public ListFilter ListFilter => Settings.Instance.ListFilter;

        public ReactiveProperty<PlayingStatus> PlayingStatus { get; } = new ReactiveProperty<PlayingStatus>(dxplayer.PlayingStatus.IDLE);
        public ReadOnlyReactiveProperty<bool> Playing { get; }
        public ReadOnlyReactiveProperty<bool> Checking { get; }
        #endregion

        public MainViewModel() {
            ItemCount = MainList.Select((c)=>c.Count).ToReadOnlyReactiveProperty<int>();
            Playing = PlayingStatus.Select((c) => c == dxplayer.PlayingStatus.PLAYING).ToReadOnlyReactiveProperty();
            Checking = PlayingStatus.Select((c) => c == dxplayer.PlayingStatus.CHECKING).ToReadOnlyReactiveProperty();
            CheckedFilterCommand.Subscribe((param) => ListFilter.Checked = misc.Utils.ParseToEnum(param, ListFilter.BoolFilter.NONE));
            CountFilterCommand.Subscribe((param) => ListFilter.PlayCountCP = misc.Utils.ParseToEnum(param, ListFilter.Comparison.NONE));
            CommandManager = new MainCommands(this);
        }

        public override void Dispose() {
            base.Dispose();
        }
    }
}
