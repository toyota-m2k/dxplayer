using System;
using System.Windows.Input;

namespace dxplayer.misc {
    public class SimpleCommand : ICommand {
        private Action Action;
        private Func<bool> Func;
        private bool mEnabled = true;
        public virtual bool Enabled {
            get => mEnabled;
            set {
                if(mEnabled!=value) {
                    mEnabled = value;
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }



        public event EventHandler CanExecuteChanged;

        public SimpleCommand(Action action) {
            Action = action;
        }
        public SimpleCommand(Func<bool> action) {
            Func = action;
        }

        public bool CanExecute(object parameter) {
            return Enabled;
        }

        public void Execute(object parameter) {
            Action?.Invoke();
            Func?.Invoke();
        }
    }
}
