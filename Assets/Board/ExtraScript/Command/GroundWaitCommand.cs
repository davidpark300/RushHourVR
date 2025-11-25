using UnityEngine;
using System;

public class GroundWaitCommand : ICommand
{
    private string _commandId;
    private StaticClock _clock;
    private Func<bool> _ender;

    public GroundWaitCommand(string commandId, StaticClock clock, Func<bool> ender)
    {
        _commandId = commandId;
        _clock = clock;
        _ender = ender;
    }

    StateManager<FrameState> _frameState = null;

    // ICommand
    public string CommandId
    {
        get { return _commandId; }
    }
    public int Execute(GameObjectContext context)
    {
        if (_frameState == null) _frameState = new(context.GetComponents<FrameState>(), GlobalConfig.Key.STATE_ID_IDLE, true);

        if (_frameState.currentState != null)
        {
            _frameState.currentState.DrawFrame();
        }

        if (_ender() == true) return CommandQueue.END;

        if (_clock.CheckAndTick(Time.deltaTime) == true)
        {
            return CommandQueue.END;
        }

        return CommandQueue.PROCESS;
    }
}
