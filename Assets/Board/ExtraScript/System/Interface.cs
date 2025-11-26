using UnityEngine;

public interface IScan<T>
{
    T GetCurrent();
    void Next();
    T GetCurrentAndNext();
    int Index { get; set; }
}

public interface IState
{
    string StateId { get; }
    void Initialize();
}

public interface ICommand
{
    string CommandId { get; }
    int Execute(GameObjectContext context);
}

public interface ISensor<TResult>
{
    string SensorId { get; }
    TResult Check { get; }
}
