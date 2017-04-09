using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StateMachineDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            StateMachine machine = new StateMachine();

            machine.OnBeforeInit += (sender, e) => { Console.WriteLine("before init"); };
            machine.OnBeforeSearch += (sender, e) => { Console.WriteLine("before search"); };
            machine.OnBeforeWait += (sender, e) => { Console.WriteLine("before wait"); };
            machine.OnBeforeConnect += (sender, e) => { Console.WriteLine("before connect"); };
            machine.OnBeforeTransmit += (sender, e) => { Console.WriteLine("before transmit"); };
            machine.OnBeforeClose += (sender, e) => { Console.WriteLine("before close"); };
            machine.OnAfterInit += (sender, e) => { Console.WriteLine("after init"); };
            machine.OnAfterSearch += (sender, e) => { Console.WriteLine("after search"); };
            machine.OnAfterWait += (sender, e) => { Console.WriteLine("after wait"); };
            machine.OnAfterConnect += (sender, e) => { Console.WriteLine("after connect"); };
            machine.OnAfterTransmit += (sender, e) => { Console.WriteLine("after transmit"); };
            machine.OnAfterClose += (sender, e) => { Console.WriteLine("after close"); };

            Console.WriteLine(machine.State);
            machine.DoAction(StateMachine.Actions.INIT);
            Console.WriteLine(machine.State);
            machine.DoAction(StateMachine.Actions.CONNECT);
            Console.WriteLine(machine.State);
            machine.DoAction(StateMachine.Actions.SEARCH);
            Console.WriteLine(machine.State);
            machine.DoAction(StateMachine.Actions.CONNECT);
            Console.WriteLine(machine.State);
            machine.DoAction(StateMachine.Actions.CLOSE);
            Console.WriteLine(machine.State);
        }
    }


    class StateMachine
    {
        public enum States
        {
            INIT,
            SEARCHING,
            WAITING,
            CONNECTED,
            TRANSMITTING,
            CLOSED
        }

        public enum Actions
        {
            INIT,
            SEARCH,
            WAIT,
            CONNECT,
            TRANSMIT,
            CLOSE
        }

        public class ActionRule
        {
            private States _from;
            public States From
            {
                get
                {
                    return _from;
                }
            }

            private States _to;
            public States To
            {
                get
                {
                    return _to;
                }
            }

            public ActionRule(States from, States to)
            {
                _from = from;
                _to = to;
            }
        }

        private Dictionary<Actions, List<ActionRule>> _actionMap = new Dictionary<Actions, List<ActionRule>>();

        private States _state;
        public States State
        {
            get
            {
                return _state;
            }
        }


        public event EventHandler<EventArgs> OnBeforeInit
        {
            add
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.INIT] += value;
                }
            }
            remove
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.INIT] -= value;
                }
            }
        }
        public event EventHandler<EventArgs> OnBeforeSearch
        {
            add
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.SEARCHING] += value;
                }
            }
            remove
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.SEARCHING] -= value;
                }
            }
        }
        public event EventHandler<EventArgs> OnBeforeWait
        {
            add
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.WAITING] += value;
                }
            }
            remove
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.WAITING] -= value;
                }
            }
        }
        public event EventHandler<EventArgs> OnBeforeConnect
        {
            add
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.CONNECTED] += value;
                }
            }
            remove
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.CONNECTED] -= value;
                }
            }
        }
        public event EventHandler<EventArgs> OnBeforeTransmit
        {
            add
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.TRANSMITTING] += value;
                }
            }
            remove
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.TRANSMITTING] -= value;
                }
            }
        }
        public event EventHandler<EventArgs> OnBeforeClose
        {
            add
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.CLOSED] += value;
                }
            }
            remove
            {
                lock (_beforeStateChangeEvents)
                {
                    _beforeStateChangeEvents[States.CLOSED] -= value;
                }
            }
        }

        public event EventHandler<EventArgs> OnAfterInit
        {
            add
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.INIT] += value;
                }
            }
            remove
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.INIT] -= value;
                }
            }
        }
        public event EventHandler<EventArgs> OnAfterSearch
        {
            add
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.SEARCHING] += value;
                }
            }
            remove
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.SEARCHING] -= value;
                }
            }
        }
        public event EventHandler<EventArgs> OnAfterWait
        {
            add
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.WAITING] += value;
                }
            }
            remove
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.WAITING] -= value;
                }
            }
        }
        public event EventHandler<EventArgs> OnAfterConnect
        {
            add
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.CONNECTED] += value;
                }
            }
            remove
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.CONNECTED] -= value;
                }
            }
        }
        public event EventHandler<EventArgs> OnAfterTransmit
        {
            add
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.TRANSMITTING] += value;
                }
            }
            remove
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.TRANSMITTING] -= value;
                }
            }
        }
        public event EventHandler<EventArgs> OnAfterClose
        {
            add
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.CLOSED] += value;
                }
            }
            remove
            {
                lock (_afterStateChangeEvents)
                {
                    _afterStateChangeEvents[States.CLOSED] -= value;
                }
            }
        }

        private Dictionary<States, EventHandler<EventArgs>> _beforeStateChangeEvents = new Dictionary<States, EventHandler<EventArgs>>();
        private Dictionary<States, EventHandler<EventArgs>> _afterStateChangeEvents = new Dictionary<States, EventHandler<EventArgs>>();

        public StateMachine()
        {
            _state = States.CLOSED;

            _actionMap.Add(Actions.INIT, new List<ActionRule> { new ActionRule(States.CLOSED, States.INIT) });
            _actionMap.Add(Actions.SEARCH, new List<ActionRule> { new ActionRule(States.INIT, States.SEARCHING) });
            _actionMap.Add(Actions.WAIT, new List<ActionRule> { new ActionRule(States.SEARCHING, States.WAITING) });
            _actionMap.Add(Actions.CONNECT,
                new List<ActionRule>
                {
                    new ActionRule(States.SEARCHING, States.CONNECTED),
                    new ActionRule(States.WAITING, States.CONNECTED),
                    new ActionRule(States.TRANSMITTING, States.CONNECTED),
                });
            _actionMap.Add(Actions.TRANSMIT, new List<ActionRule> { new ActionRule(States.CONNECTED, States.TRANSMITTING) });
            _actionMap.Add(Actions.CLOSE,
                new List<ActionRule>
                {
                    new ActionRule(States.INIT, States.CLOSED),
                    new ActionRule(States.SEARCHING, States.CLOSED),
                    new ActionRule(States.WAITING, States.CLOSED),
                    new ActionRule(States.CONNECTED, States.CLOSED),
                    new ActionRule(States.TRANSMITTING, States.CLOSED),
                });

            _beforeStateChangeEvents.Add(States.INIT, null);
            _beforeStateChangeEvents.Add(States.SEARCHING, null);
            _beforeStateChangeEvents.Add(States.WAITING, null);
            _beforeStateChangeEvents.Add(States.CONNECTED, null);
            _beforeStateChangeEvents.Add(States.TRANSMITTING, null);
            _beforeStateChangeEvents.Add(States.CLOSED, null);

            _afterStateChangeEvents.Add(States.INIT, null);
            _afterStateChangeEvents.Add(States.SEARCHING, null);
            _afterStateChangeEvents.Add(States.WAITING, null);
            _afterStateChangeEvents.Add(States.CONNECTED, null);
            _afterStateChangeEvents.Add(States.TRANSMITTING, null);
            _afterStateChangeEvents.Add(States.CLOSED, null);
        }

        public void AddActionRules(Actions action, List<ActionRule> rules)
        {
            _actionMap.Add(action, rules);
        }

        public bool DoAction(Actions action)
        {
            List<ActionRule> actionRules = _actionMap[action];
            foreach (var rule in actionRules)
            {
                if (rule.From == this._state)
                {
                    if(_beforeStateChangeEvents.ContainsKey(rule.To) && _beforeStateChangeEvents[rule.To] != null)
                    {
                        _beforeStateChangeEvents[rule.To](null, null);
                    }
                    this._state = rule.To;
                    Console.WriteLine("changing state from {0} to {1}", rule.From, rule.To);
                    if (_afterStateChangeEvents.ContainsKey(rule.To) && _afterStateChangeEvents[rule.To] != null)
                    {
                        _afterStateChangeEvents[rule.To](null, null);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
