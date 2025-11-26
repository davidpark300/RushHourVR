using System;
using System.Collections.Generic;
using UnityEngine;

public class VectorSettingCommand : ICommand
{
    private string _commandId;

    private List<float> _startVector;
    private bool startFromThis = false;
 
    public delegate float Getter(GameObjectContext context);
    private List<Getter> DataGetter;

    private List<float> _endVector;

    public delegate int Setter(GameObjectContext context, List<float> param);
    private Setter DataSetter;

    private StaticClock _clock;

    private List<float> _currentVector = new();

    public VectorSettingCommand(string commandId,
        List<float> startVector, List<Getter> DataGetter,
        List<float> endVector, Setter DataSetter,
        StaticClock clock)
    {
        _commandId = commandId;
        _startVector = startVector;
        this.DataGetter = DataGetter;
        _endVector = endVector;
        this.DataSetter = DataSetter;
        _clock = clock;

        if (startVector == null || DataGetter == null || DataGetter == null || endVector == null || _clock == null)
        {
            GlobalLogger.Error("null", _commandId, nameof(VectorSettingCommand), "startVector == null || DataGetter == null || DataGetter == null || endVector == null || _clock == null");
            return;
        }

        if (startVector.Count == 0 && (DataGetter.Count == 0 || (DataGetter.Count != endVector.Count)))
        {
            GlobalLogger.Error("null", _commandId, nameof(VectorSettingCommand), "startVector.Count == 0 && ( DataGetter.Count == 0 || (DataGetter.Count != endVector.Count) )");
            return;
        }

        if (startVector.Count != 0 && (DataGetter.Count != 0 || (startVector.Count != endVector.Count)))
        {
            GlobalLogger.Error("null", _commandId, nameof(VectorSettingCommand), "startVector.Count != 0 && ( DataGetter.Count != 0 || (startVector.Count != endVector.Count) )");
            return;
        }

        if (startVector.Count == 0) startFromThis = true;
    }

    // ICommand
    public string CommandId
    {
        get { return _commandId; }
    }
    public int Execute(GameObjectContext context)
    {
        if (_clock.IsInfinite)
        {
            _currentVector = _endVector;
            return DataSetter(context, _currentVector);
        }

        if (startFromThis)
        {
            foreach (var dataGetter in DataGetter)
            {
                _startVector.Add(dataGetter(context));
            }
            startFromThis = false;
        }

        if (_clock.Check() || _clock.Time == 0)
        {
            _currentVector = _endVector;
            if (DataSetter(context, _currentVector) == CommandQueue.ERROR) return CommandQueue.ERROR;
            return CommandQueue.END;
        }

        _currentVector = new List<float>(new float[_endVector.Count]);
        for (int i = 0; i < _startVector.Count; ++i)
        {
            _currentVector[i] = _startVector[i] * (1f - _clock.Rate) + _endVector[i] * _clock.Rate;
        }
        _clock.Tick(Time.deltaTime);

        return DataSetter(context, _currentVector);
    }
}
