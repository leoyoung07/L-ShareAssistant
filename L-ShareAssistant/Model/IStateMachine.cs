using System;

namespace L_ShareAssistant.Model
{
    public interface IStateMachine
    {
        StateMachine.States State { get; }

        event EventHandler<EventArgs> OnAfterClose;
        event EventHandler<EventArgs> OnAfterConnect;
        event EventHandler<EventArgs> OnAfterInit;
        event EventHandler<EventArgs> OnAfterSearch;
        event EventHandler<EventArgs> OnAfterTransmit;
        event EventHandler<EventArgs> OnAfterWait;
        event EventHandler<EventArgs> OnBeforeClose;
        event EventHandler<EventArgs> OnBeforeConnect;
        event EventHandler<EventArgs> OnBeforeInit;
        event EventHandler<EventArgs> OnBeforeSearch;
        event EventHandler<EventArgs> OnBeforeTransmit;
        event EventHandler<EventArgs> OnBeforeWait;
        event EventHandler<EventArgs> OnStateChange;

        bool DoAction(StateMachine.Actions action);
    }
}