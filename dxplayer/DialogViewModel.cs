using dxplayer.server;
using dxplayer.settings;
using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace dxplayer {
    public class DialogViewModel : ViewModelBase {
        public enum DialogType {
            NONE,
            SETTINGS,
        }
        public ReactivePropertySlim<string> Title { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<DialogType> Type { get; } = new ReactivePropertySlim<DialogType>(DialogType.NONE);
        public ReadOnlyReactivePropertySlim<bool> Showing { get; }

        public ReactiveCommand OkCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CancelCommand { get; } = new ReactiveCommand();

        private TaskCompletionSource<bool> Completion = null;

        public DialogViewModel() {
            Showing = Type.Select(c => c != DialogType.NONE).ToReadOnlyReactivePropertySlim();
            OkCommand.Subscribe(() => Close(true));
            CancelCommand.Subscribe(() => Close(false));
        }

        interface IDialogViewModel {
            string TITLE { get; }
            DialogType TYPE { get; }
        }

        private void Close(bool result) {
            Completion?.TrySetResult(result);
            Completion = null;
            Type.Value = DialogType.NONE;
        }

        private async Task<bool> Show(IDialogViewModel dv) {
            Completion = new TaskCompletionSource<bool>();
            Title.Value = dv.TITLE;
            Type.Value = dv.TYPE;
            return await Completion.Task;
        }

        public class SettingDialogViewModel : ViewModelBase, IDialogViewModel {
            public string TITLE => "Settings";
            public DialogType TYPE => DialogType.SETTINGS;

            public ReactivePropertySlim<string> DxxDBPath { get; } = new ReactivePropertySlim<string>();
            public ReactivePropertySlim<bool> UseServer { get; } = new ReactivePropertySlim<bool>();
            public ReactivePropertySlim<bool> PlayCountFromServer { get; } = new ReactivePropertySlim<bool>();
            public ReactivePropertySlim<int> ServerPort { get; } = new ReactivePropertySlim<int>();
            public ReactiveCommand RefDxxDBPathCommand { get; } = new ReactiveCommand();
        }

        public SettingDialogViewModel SettingDialog { get; } = new SettingDialogViewModel();
        public async Task<bool> ShowSettingDialog() {
            if (Completion != null) return false;

            SettingDialog.DxxDBPath.Value = Settings.Instance.DxxDBPath;
            SettingDialog.UseServer.Value = Settings.Instance.UseServer;
            SettingDialog.ServerPort.Value = Settings.Instance.ServerPort;
            SettingDialog.PlayCountFromServer.Value = Settings.Instance.PlayCountFromServer;

            if (!await Show(SettingDialog)) { 
                return false;
            }
            Settings.Instance.DxxDBPath = SettingDialog.DxxDBPath.Value;
            Settings.Instance.UseServer = SettingDialog.UseServer.Value;
            Settings.Instance.ServerPort = SettingDialog.ServerPort.Value;
            Settings.Instance.PlayCountFromServer = SettingDialog.PlayCountFromServer.Value;
            Settings.Instance.Serialize();
            return true;
        }
    }
}
