using dxplayer.misc;
using io.github.toyota32k.toolkit.view;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using KeyCommandManager = dxplayer.misc.KeyCommandManager;

namespace dxplayer {
    public class MainCommands : KeyCommandManager {
        //[Disposal(false)]
        //private MainViewModel ViewModel;
        
        public MainCommands(MainViewModel viewModel) {
            AddSingleKeyCommand(Key.Enter, viewModel.PreviewCommand.ToCommand("Play to check"));       // return == enter
            AddControlKeyCommand(Key.G, viewModel.PlayCommand.ToCommand("Play"));
            AddControlKeyCommand(Key.J, viewModel.CheckCommand.ToCommand("Check"));
            AddControlShiftKeyCommand(Key.J, viewModel.UncheckCommand.ToCommand("Uncheck"));
            AddControlKeyCommand(Key.T, viewModel.DecrementCounterCommand.ToCommand("Decrement counter"));

            //AddSingleKeyCommand(Key.Return, viewModel.PreviewCommand.ToCommand());
        }
    }
}
