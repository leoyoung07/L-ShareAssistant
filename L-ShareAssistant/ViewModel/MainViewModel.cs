using GalaSoft.MvvmLight;
using L_ShareAssistant.Model;
using GalaSoft.MvvmLight.CommandWpf;
using System.Windows.Input;

namespace L_ShareAssistant.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IStateMachine _stateMachine;

        /// <summary>
        /// The <see cref="WelcomeTitle" /> property's name.
        /// </summary>
        public const string WelcomeTitlePropertyName = "WelcomeTitle";

        private string _welcomeTitle = string.Empty;

        /// <summary>
        /// Gets the WelcomeTitle property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// </summary>
        public string WelcomeTitle
        {
            get
            {
                return _welcomeTitle;
            }
            set
            {
                Set(ref _welcomeTitle, value);
            }
        }

        public ICommand TestCommand { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
            _stateMachine.OnStateChange += _stateMachine_OnStateChange;
            WelcomeTitle = _stateMachine.State.ToString();

            TestCommand = new RelayCommand(() =>
            {
                switch (_stateMachine.State)
                {
                    case StateMachine.States.INIT:
                        _stateMachine.DoAction(StateMachine.Actions.SEARCH);
                        break;
                    case StateMachine.States.SEARCHING:
                        _stateMachine.DoAction(StateMachine.Actions.WAIT);
                        break;
                    case StateMachine.States.WAITING:
                        _stateMachine.DoAction(StateMachine.Actions.CONNECT);
                        break;
                    case StateMachine.States.CONNECTED:
                        _stateMachine.DoAction(StateMachine.Actions.TRANSMIT);
                        break;
                    case StateMachine.States.TRANSMITTING:
                        _stateMachine.DoAction(StateMachine.Actions.CLOSE);
                        break;
                    case StateMachine.States.CLOSED:
                        _stateMachine.DoAction(StateMachine.Actions.INIT);
                        break;
                    default:
                        _stateMachine.DoAction(StateMachine.Actions.CLOSE);
                        break;
                }
            });
        }

        private void _stateMachine_OnStateChange(object sender, System.EventArgs e)
        {
            WelcomeTitle = _stateMachine.State.ToString();
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}