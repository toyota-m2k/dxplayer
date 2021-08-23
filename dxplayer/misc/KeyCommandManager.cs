using io.github.toyota32k.toolkit.utils;
using io.github.toyota32k.toolkit.view;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace dxplayer.misc {
    public class Command {
        private string Name;
        private object Executable;

        private Command(string name) {
            Executable = null;
            Name = name;
        }

        public Command(string name, Action fn) {
            Executable = fn;
            Name = name;
        }
        public Command(string name, ReactiveCommand fn) {
            Executable = fn;
            Name = name;
        }
        public static Command Create(string name, Action fn) {
            return new Command(name, fn);
        }
        public static Command From(string name, ReactiveCommand fn) {
            return new Command(name, fn);
        }

        public void Invoke() {
            if (Executable == null) return;
            var action = Executable as Action;
            if(action!=null) {
                action();
                return;
            }
            var command = Executable as ReactiveCommand;
            if (command != null) {
                command.Execute();
                return;
            }
            Logger.error("invalid executable");
        }

        public static Command NOP = new Command("NOP");
    }

    public class KeyCommandManager : ViewModelBase {
        private Dictionary<Key, Command> SingleKeyCommands = new Dictionary<Key, Command>();
        private Dictionary<Key, Command> ControlKeyCommands = new Dictionary<Key, Command>();
        private Dictionary<Key, Command> ShiftKeyCommands = new Dictionary<Key, Command>();
        private Dictionary<Key, Command> ControlShiftKeyCommands = new Dictionary<Key, Command>();

        private ReactiveProperty<Key> ActiveKey { get; } = new ReactiveProperty<Key>(Key.None/*, ReactivePropertyMode.DistinctUntilChanged*/);
        private ReactiveProperty<bool> Ctrl { get; } = new ReactiveProperty<bool>(false/*, ReactivePropertyMode.DistinctUntilChanged*/);
        private ReactiveProperty<bool> Shift { get; } = new ReactiveProperty<bool>(false/*, ReactivePropertyMode.DistinctUntilChanged*/);
        private IDisposable enabled { get; set; } = null;

        public ReadOnlyReactiveProperty<Command> Command { get; }
        public Action OnCommandEnd = null;

        public KeyCommandManager() : base(disposeNonPublic:true) {
            Command = ActiveKey.CombineLatest(Ctrl, Shift, (k, c, s) => {
                if (c && s) {
                    return ControlShiftKeyCommands.GetValue(k, null);
                }
                else if (c) {
                    return ControlKeyCommands.GetValue(k, null);
                }
                else if (s) {
                    return ShiftKeyCommands.GetValue(k, null);
                }
                else {
                    return SingleKeyCommands.GetValue(k, null);
                }
            }).ToReadOnlyReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged);
        }

        public bool Enabled {
            get => enabled != null;
        }
        private void OnKeyDown(object sender, KeyEventArgs e) {
            //LoggerEx.debug($"Key={e.Key}, Sys={e.SystemKey}, State={e.KeyStates}, Rep={e.IsRepeat}, Down={e.IsDown}, Up={e.IsUp}, Toggled={e.IsToggled}");
            Down(e.Key);
        }
        private void OnKeyUp(object sender, KeyEventArgs e) {
            //LoggerEx.debug($"Key={e.Key}, Sys={e.SystemKey}, State={e.KeyStates}, Rep={e.IsRepeat}, Down={e.IsDown}, Up={e.IsUp}, Toggled={e.IsToggled}");
            Up(e.Key);
        }

        public void Enable(Window owner, bool enable) {
            if (enable) {
                if (null == enabled) {
                    Cancel();
                    owner.AddHandler(Keyboard.PreviewKeyDownEvent, (KeyEventHandler)OnKeyDown);
                    owner.AddHandler(Keyboard.PreviewKeyUpEvent, (KeyEventHandler)OnKeyUp);
                    enabled = Command.Subscribe(c => {
                        Execute(c);
                    });
                }
            } else {
                if (null != enabled) {
                    owner.RemoveHandler(Keyboard.PreviewKeyDownEvent, (KeyEventHandler)OnKeyDown);
                    owner.RemoveHandler(Keyboard.PreviewKeyUpEvent, (KeyEventHandler)OnKeyUp);
                    enabled.Dispose();
                    enabled = null;
                    Cancel();
                }
            }
        }

        private void Execute(Command obj) {
            LoggerEx.debug($"{obj}");
            OnCommandEnd?.Invoke();
            OnCommandEnd = null;
            obj?.Invoke();
        }

        public void AddSingleKeyCommand(Key key,Command command) {
            SingleKeyCommands.Add(key, command);
        }
        public void AddControlKeyCommand(Key key, Command command) {
            ControlKeyCommands.Add(key, command);
        }
        public void AddShiftKeyCommand(Key key, Command command) {
            ShiftKeyCommands.Add(key, command);
        }
        public void AddControlShiftKeyCommand(Key key, Command command) {
            ControlShiftKeyCommands.Add(key, command);
        }

        public void Down(Key key) {
            LoggerEx.debug($"{key}");
            if (key == Key.LeftCtrl || key == Key.RightCtrl) {
                Ctrl.Value = true;
                LoggerEx.debug($"Ctrl=true");
            }
            else if (key == Key.LeftShift || key == Key.RightShift) {
                Shift.Value = true;
                LoggerEx.debug($"Shift=true");
            }
            else {
                LoggerEx.debug($"{ActiveKey.Value} --> {key}");
                ActiveKey.Value = key;
            }
            LoggerEx.debug($"Ctrl={Ctrl.Value}, Shift={Shift.Value}, {ActiveKey.Value}");
        }

        public void Up(Key key) {
            if (key == Key.LeftCtrl || key == Key.RightCtrl) {
                Ctrl.Value = false;
                LoggerEx.debug($"Ctrl=false");
            }
            if (key == Key.LeftShift || key==Key.RightShift) {
                Shift.Value = false;
                LoggerEx.debug($"Shift=false");
            }
            else if (key == ActiveKey.Value) {
                ActiveKey.Value = Key.None;
                LoggerEx.debug($"{key} --> None");
            }
            LoggerEx.debug($"Ctrl={Ctrl.Value}, Shift={Shift.Value}, {ActiveKey.Value}");
        }

        public void Cancel() {
            ActiveKey.Value = Key.None;
            Ctrl.Value = false;
            Shift.Value = false;
        }

        public override void Dispose() {
            base.Dispose();
        }
    }

    public static class CommandExtension {
        public static Command ToCommand(this Action fn, string name) {
            return new Command(name, fn);
        }
        public static Command ToCommand(this ReactiveCommand fn, string name) {
            return new Command(name, fn);
        }
    }
}
