using UnityEngine;
using System;

public class BlockCommand : ICommand
{
    private string _commandId;
    private StaticClock _clock;

    public BlockCommand(string commandId, StaticClock clock)
    {
        _commandId = commandId;
        _clock = clock;
    }

    // ICommand
    public string CommandId
    {
        get { return _commandId; }
    }
    public int Execute(GameObjectContext context)
    {
        if (_clock.CheckAndTick(Time.deltaTime) == true)
        {
            return CommandQueue.END;
        }

        return CommandQueue.PROCESS;
    }
}
