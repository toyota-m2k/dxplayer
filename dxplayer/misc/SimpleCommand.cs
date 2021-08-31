using System;
using System.Windows.Input;

namespace dxplayer.misc {
    public class SimpleCommand : ICommand {
        private Action Action;
        private Func<bool> Func;
        public event EventHandler CanExecuteChanged;

        public SimpleCommand(Action action) {
            Action = action;
        }
        public SimpleCommand(Func<bool> action) {
            Func = action;
        }

        public bool CanExecute(object parameter) {
            return true;
        }

        public void Execute(object parameter) {
            Action?.Invoke();
            Func?.Invoke();
        }
    }
}
