using dxplayer.data.main;
using dxplayer.ffmpeg;
using dxplayer.settings;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace dxplayer {
    public class DialogViewModel : ViewModelBase {
        public enum DialogType {
            NONE,
            SETTINGS,
            COMPRESS_PROGRESS,
        }
        public ReactivePropertySlim<string> Title { get; } = new ReactivePropertySlim<string>();
        public ReactivePropertySlim<DialogType> Type { get; } = new ReactivePropertySlim<DialogType>(DialogType.NONE);
        public ReadOnlyReactivePropertySlim<bool> Showing { get; }

        public ReactivePropertySlim<bool> OkVisible { get; } = new ReactivePropertySlim<bool>(true);
        public ReactivePropertySlim<bool> CancelVisible { get; } = new ReactivePropertySlim<bool>(true);
        public ReactiveCommand OkCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CancelCommand { get; } = new ReactiveCommand();

        private TaskCompletionSource<bool> Completion = null;

        public DialogViewModel() {
            Showing = Type.Select(c => c != DialogType.NONE).ToReadOnlyReactivePropertySlim();
        }

        interface IDialogViewModel {
            string TITLE { get; }
            DialogType TYPE { get; }
        }

        private async Task<bool> Show(IDialogViewModel dv) {
            Completion = new TaskCompletionSource<bool>();
            Title.Value = dv.TITLE;
            Type.Value = dv.TYPE;
            OkVisible.Value = true;
            CancelVisible.Value = true;
            OkCommand.Subscribe(() => Close(true));
            CancelCommand.Subscribe(() => Close(false));
            return await Completion.Task;
        }

        private void Close(bool result) {
            Completion?.TrySetResult(result);
            Completion = null;
            Type.Value = DialogType.NONE;
            OkCommand.Dispose();
            CancelCommand.Dispose();
        }

        #region Settting Dialog

        public class SettingDialogViewModel : ViewModelBase, IDialogViewModel {
            public string TITLE => "Settings";
            public DialogType TYPE => DialogType.SETTINGS;

            public ReactivePropertySlim<string> FFMpegPath { get; } = new ReactivePropertySlim<string>();
            public ReactivePropertySlim<string> DxxDBPath { get; } = new ReactivePropertySlim<string>();
            public ReactivePropertySlim<bool> UseServer { get; } = new ReactivePropertySlim<bool>();
            public ReactivePropertySlim<bool> PlayCountFromServer { get; } = new ReactivePropertySlim<bool>();
            public ReactivePropertySlim<int> ServerPort { get; } = new ReactivePropertySlim<int>();
            public ReactiveCommand RefDxxDBPathCommand { get; } = new ReactiveCommand();
            public ReactiveCommand RefFFMpegPathCommand { get; } = new ReactiveCommand();
        }

        public SettingDialogViewModel SettingDialog { get; } = new SettingDialogViewModel();
        public async Task<bool> ShowSettingDialog() {
            if (Completion != null) return false;
            if (Type.Value != DialogType.NONE) return false;

            SettingDialog.DxxDBPath.Value = Settings.Instance.DxxDBPath;
            SettingDialog.FFMpegPath.Value = Settings.Instance.FFMpegPath;
            SettingDialog.UseServer.Value = Settings.Instance.UseServer;
            SettingDialog.ServerPort.Value = Settings.Instance.ServerPort;
            SettingDialog.PlayCountFromServer.Value = Settings.Instance.PlayCountFromServer;

            if (!await Show(SettingDialog)) { 
                return false;
            }
            Settings.Instance.DxxDBPath = SettingDialog.DxxDBPath.Value;
            Settings.Instance.FFMpegPath = SettingDialog.FFMpegPath.Value;
            Settings.Instance.UseServer = SettingDialog.UseServer.Value;
            Settings.Instance.ServerPort = SettingDialog.ServerPort.Value;
            Settings.Instance.PlayCountFromServer = SettingDialog.PlayCountFromServer.Value;
            Settings.Instance.Serialize();
            return true;
        }
        #endregion

        #region Compress Progress Dialog
        public class CompressProgressViewModel : ViewModelBase, IDialogViewModel, IFFProgress {
            public string TITLE => "Compressor";
            public DialogType TYPE => DialogType.COMPRESS_PROGRESS;

            private ReactivePropertySlim<List<PlayItem>> TargetItems { get; } = new ReactivePropertySlim<List<PlayItem>>();

            public ReactivePropertySlim<int> ItemCount { get; } = new ReactivePropertySlim<int>();
            public ReactivePropertySlim<int> CurrentItemIndex { get; } = new ReactivePropertySlim<int>();
            
            public ReadOnlyReactivePropertySlim<bool> MultiItem { get; }
            public ReadOnlyReactivePropertySlim<double> ItemPercent { get; }

            public ReadOnlyReactivePropertySlim<PlayItem> CurrentItem { get; }

            public ReadOnlyReactivePropertySlim<string> ItemMessage { get; }
            public ReadOnlyReactivePropertySlim<string> TargetItemName { get; }


            public ReadOnlyReactivePropertySlim<string> ProcessLabel { get; }
            public ReactivePropertySlim<bool> Alive { get; } = new ReactivePropertySlim<bool>();

            private ReactivePropertySlim<FFProcessId> _processId = new ReactivePropertySlim<FFProcessId>();
            FFProcessId IFFProgress.ProcessId {
                get => _processId.Value;
                set {
                    _processId.Value = value;
                }
            }
            public ReactivePropertySlim<double> ProcessPercent { get; } = new ReactivePropertySlim<double>();
            double IFFProgress.Percent {
                get => ProcessPercent.Value;
                set {
                    ProcessPercent.Value = value;
                }
            }
            public ReactivePropertySlim<string> ProcessMessage { get; } = new ReactivePropertySlim<string>();
            string IFFProgress.ProgressMessage {
                get => ProcessMessage.Value;
                set {
                    ProcessMessage.Value = value;
                }
            }

            public void SetItemList(List<PlayItem> items) {
                TargetItems.Value = items;
                ItemCount.Value = items.Count;
                CurrentItemIndex.Value = 0;
            }

            public CompressProgressViewModel() {
                MultiItem = ItemCount.Select(c => c > 1).ToReadOnlyReactivePropertySlim();
                ItemPercent = CurrentItemIndex.CombineLatest(ItemCount, (index,count) => (count>0) ? (double)(index+1)*100 / count : 0).ToReadOnlyReactivePropertySlim();
                CurrentItem = CurrentItemIndex.CombineLatest(TargetItems, (index,items)=> (items!=null && 0<=index && index<items.Count) ? items[index] : null).ToReadOnlyReactivePropertySlim();
                ItemMessage = CurrentItemIndex.CombineLatest(CurrentItem,ItemCount, (index,item,count) => $"{index + 1}/{count} - {item?.Name??""}").ToReadOnlyReactivePropertySlim();
                TargetItemName = CurrentItem.Select(c => c?.Name ?? "").ToReadOnlyReactivePropertySlim();
                ProcessLabel = _processId.Select(c => {
                    switch (c) {
                        case FFProcessId.TRIMMING: return "Trimming...";
                        case FFProcessId.SPLITTING: return "Splitting...";
                        case FFProcessId.COMBINING: return "Combining...";
                        case FFProcessId.COMPRESSING: return "Compressing...";
                        case FFProcessId.DISPOSING: return "Disposing...";
                        default: return "";
                    }
                }).ToReadOnlyReactivePropertySlim();
            }
        }
        public CompressProgressViewModel Compress { get; } = new CompressProgressViewModel();

        public async Task ShowCompressProgress(List<PlayItem> items) {
            if(Type.Value != DialogType.NONE) return;
            Compress.SetItemList(items);
            Title.Value = Compress.TITLE;
            Type.Value = Compress.TYPE;
            OkVisible.Value = false;
            Compress.Alive.Value = true;
            CancelVisible.Value = true;
            CancelCommand.Subscribe(() => {
                Compress.Alive.Value = false;
            });
            for(int i = 0; Compress.Alive.Value && i < items.Count; i++) {
                Compress.CurrentItemIndex.Value = i;
                var item = items[i];
                await item.Compress(Compress);
            }
            Type.Value = DialogType.NONE;
            CancelCommand.Dispose();
        }
        public async Task ShowCompressProgress(params PlayItem[] items) {
            await ShowCompressProgress(items.ToList());
        }
        #endregion
    }
}
