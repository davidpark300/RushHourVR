#nullable enable

using UnityEngine;
using System;
using System.Collections.Generic;

public class Counter
{
    private int _count;
    public Counter(int count = 0)
    {
        _count = count;
    }
    public int count
    {
        get => _count;
        set => _count = value;
    }
    public int PreIncrease(int value)
    {
        count = count + value;
        return count;
    }
    public int PostIncrease(int value)
    {
        int tmp = count;
        count = count + value;
        return tmp;
    }
    public int PreDecrease(int value)
    {
        count = count - value;
        return count;
    }
    public int PostDecrease(int value)
    {
        int tmp = count;
        count = count - value;
        return tmp;
    }
}
public class BoundedCounter
{
    private Counter _count;
    private int _upperBound;
    private int _lowerBound;

    public BoundedCounter(int upperBound, int lowerBound, int count = 0)
    {
        _count = new(count);
        _upperBound = upperBound;
        _lowerBound = lowerBound;
    }

    public int count
    {
        get => _count.count;
        set => _count.count = Math.Clamp(value, _lowerBound, _upperBound);
    }

    public int PreIncrease(int value)
    {
        count = Math.Min(_upperBound, count + value);
        return count;
    }

    public int PostIncrease(int value)
    {
        int tmp = count;
        count = Math.Min(_upperBound, count + value);
        return tmp;
    }

    public int PreDecrease(int value)
    {
        count = Math.Max(_lowerBound, count - value);
        return count;
    }

    public int PostDecrease(int value)
    {
        int tmp = count;
        count = Math.Max(_lowerBound, count - value);
        return tmp;
    }
}

public class BaseScan<T> : IScan<T>
{
    protected T[] _elements = { };
    protected Func<int, int> Indexer = (param) => param;
    protected int _index;
    public int Index
    {
        get { return _index; }
        set { _index = value; }
    }

    public BaseScan(T[] elements, Func<int, int> indexer, int index = 0)
    {
        if (elements == null)
        {
            GlobalLogger.Error("null", nameof(BaseScan<T>), nameof(BaseScan<T>), "elements == null");
            return;
        }
        if (elements.Length == 0)
        {
            GlobalLogger.Error("null", nameof(BaseScan<T>), nameof(BaseScan<T>), "elements.Lenght == 0");
            return;
        }
        if (indexer == null)
        {
            GlobalLogger.Error("null", nameof(BaseScan<T>), nameof(BaseScan<T>), "indexer == null");
            return;
        }
        _elements = elements!;
        Indexer = indexer!;
        _index = index;
    }

    public T GetCurrent()
    {
        if (_index < 0)
        {
            GlobalLogger.Error("null", nameof(BaseScan<T>), nameof(GetCurrent), "_index < 0");
            return _elements[0];
        }
        if (_index >= _elements.Length)
        {
            GlobalLogger.Error("null", nameof(BaseScan<T>), nameof(GetCurrent), "_index >= _elements.Length");
            return _elements[^1];
        }
        return _elements[_index];
    }
    public void Next()
    {
        _index = Indexer(_index);
    }
    public T GetCurrentAndNext()
    {
        T current = GetCurrent();
        Next();
        return current;
    }
}
public class Cscan<T> : BaseScan<T>
{
    public Cscan(T[] elements, int index = 0, bool increase = true)
        : base(elements, (param) =>
        {
            if (elements.Length <= 1) return 0;

            if (increase)
            {
                if (param >= elements.Length - 1) return 0;
                return param + 1;
            }
            else
            {
                if (param <= 0) return elements.Length - 1;
                return param - 1;
            }
        }, index)
    {
    }
}
public class Scan<T> : BaseScan<T>
{
    private bool _increase;

    public Scan(T[] elements, int index = 0, bool increase = true)
        : base(elements, _ => 0, index)
    {
        _increase = increase;

        Indexer = (param) =>
        {
            if (_elements.Length <= 1) return 0;

            if (_increase)
            {
                if (_index >= _elements.Length - 1)
                {
                    _increase = false;
                    return param - 1;
                }
                return param + 1;
            }
            else
            {
                if (_index <= 0)
                {
                    _increase = true;
                    return param + 1;
                }
                return param - 1;
            }
        };
    }
}

public class StateManager<TState> where TState : IState
{
    private Dictionary<string, TState> _states = new Dictionary<string, TState>();
    private string _currentStateId = "";
    public string CurrentStateId
    {
        get { return _currentStateId; }
        private set { _currentStateId = value; }
    }
    public TState? currentState
    {
        get
        {
            if (!_states.ContainsKey(_currentStateId)) return default(TState);
            return _states[_currentStateId];
        }
    }

    public StateManager(TState[] states, string defaultStateId, bool initialize = true)
    {
        if (states == null || defaultStateId == null)
        {
            GlobalLogger.Error("", nameof(StateManager<TState>), nameof(StateManager<TState>), "states == null || defaultStateId == null");
            return;
        }
        foreach (TState state in states)
        {
            state.Initialize();
            _states.Add(state.StateId, state);
        }
        _currentStateId = defaultStateId!;
    }

    public TState? FindState(string name)
    {
        if (!_states.ContainsKey(name)) return default(TState);
        return _states[name];
    }

    public void InitializeStates()
    {
        foreach (KeyValuePair<string, TState> state in _states)
        {
            state.Value.Initialize();
        }
    }

    public void InitializeState(string name)
    {
        if (_states.ContainsKey(name))
        {
            _states[name].Initialize();
        }
    }

    public void SetState(string name)
    {
        _currentStateId = name;
    }

    public void SetStateWithInitialize(string name)
    {
        if (_currentStateId == name) return;
        InitializeState(_currentStateId);
        SetState(name);
    }

    public bool TrySetState(string name)
    {
        if (!_states.ContainsKey(name)) return false;
        _currentStateId = name;
        return true;
    }

    public bool TrySetStateWithInitialize(string name)
    {
        if (!_states.ContainsKey(name)) return false;
        if (_currentStateId == name) return true;
        InitializeState(_currentStateId);
        SetState(name);
        return true;
    }

}

[System.Serializable]
public class StringPair
{
    public string? Key;
    public string? Value;
}

public class CollisionEntities
{
    private Dictionary<GameObject, Vector3> entityDictionary;
    public Dictionary<GameObject, Vector3> EntityDictionary
    {
        get { return entityDictionary; }
    }

    public CollisionEntities()
    {
        entityDictionary = new();
    }

    public void Add(Collision collision)
    {
        entityDictionary[collision.gameObject] = collision.contacts[0].normal;
    }
    public void Remove(Collision collision)
    {
        entityDictionary.Remove(collision.gameObject);
    }
    public void Clear()
    {
        entityDictionary.Clear();
    }
}
public class ColliderEntities
{
    private HashSet<GameObject> entitySet;
    public HashSet<GameObject> EntitySet
    {
        get { return entitySet; }
    }

    public ColliderEntities()
    {
        entitySet = new();
    }

    public void Add(Collider collider)
    {
        entitySet.Add(collider.gameObject);
    }
    public void Remove(Collider collider)
    {
        entitySet.Remove(collider.gameObject);
    }
    public void Clear()
    {
        entitySet.Clear();
    }
}

public class PriorityQueue<T>
{
    private readonly List<(T item, int priority, long seq)> _list = new();
    private readonly bool _minFirst;   // true: 작은 숫자 먼저, false: 큰 숫자 먼저
    private long _seqCounter = 0;      // 동일 우선순위 안정성

    public int Count => _list.Count;

    public PriorityQueue(bool minFirst = true)
    {
        _minFirst = minFirst;
    }

    public void Enqueue(T item, int priority)
    {
        // 1) 뒤에 붙이고
        _list.Add((item, priority, _seqCounter++));

        // 2) 바로 앞 요소와 순차 비교하며 한 칸씩 올리기 (삽입정렬)
        int i = _list.Count - 1;
        while (i > 0 && IsHigher(_list[i], _list[i - 1]))
        {
            // swap with previous
            (_list[i], _list[i - 1]) = (_list[i - 1], _list[i]);
            i--;
        }
    }

    public T? Dequeue()
    {
        if (_list.Count == 0)
        {
            GlobalLogger.Warning("null", nameof(PriorityQueue<T>), nameof(Dequeue), "_list.Count == 0");
            return default;
        }

        // 가장 높은 우선순위는 0번에 위치
        var item = _list[0].item;
        _list.RemoveAt(0); // 단순/확실하되, O(n)임
        return item;
    }

    public bool TryDequeue(out T result)
    {
        if (_list.Count == 0)
        {
            result = default!;
            return false;
        }
        result = Dequeue()!;
        return true;
    }

    public T? Peek()
    {
        if (_list.Count == 0)
        {
            GlobalLogger.Warning("null", nameof(PriorityQueue<T>), nameof(Peek), "_list.Count == 0");
            return default;
        }
        return _list[0].item;
    }

    public bool TryPeek(out T result)
    {
        if (_list.Count == 0)
        {
            result = default!;
            return false;
        }
        result = _list[0].item;
        return true;
    }

    // a가 b보다 “우선”이면 true
    private bool IsHigher((T item, int priority, long seq) a,
                          (T item, int priority, long seq) b)
    {
        if (a.priority != b.priority)
            return _minFirst ? a.priority < b.priority : a.priority > b.priority;

        // 동일 우선순위면 먼저 들어온 것이 앞 (안정성)
        return a.seq < b.seq;
    }
}

public class CommandEntry
{
    public ICommand command { get; set; }
    public Counter repeat { get; set; }

    public CommandEntry(ICommand command, int repeat)
    {
        this.command = command;
        this.repeat = new Counter(repeat);
    }
}

public class CommandQueue
{

    public static readonly int ERROR = -1;
    public static readonly int END = 0;
    public static readonly int PROCESS = 1;

    public static readonly int PRIORITY_HIGH = 99;
    public static readonly int PRIORITY_NORMAL = 100;
    public static readonly int PRIORITY_LOW = 101;

    public static readonly int INF = -1;

    private PriorityQueue<CommandEntry> _queue;

    public CommandQueue()
    {
        _queue = new();
    }

    public void Enqueue(ICommand command, int repeat, int priority)
    {
        _queue.Enqueue(new CommandEntry(command, repeat), priority);
    }
    public ICommand? Dequeue()
    {
        if (_queue.Count == 0)
        {
            GlobalLogger.Warning("null", nameof(CommandQueue), nameof(Dequeue), "_queue.Count == 0");
            return default(ICommand);
        }
        return _queue.Dequeue()!.command;
    }
    public ICommand? Peek()
    {
        if (_queue.Count == 0)
        {
            GlobalLogger.Warning("null", nameof(CommandQueue), nameof(Peek), "_queue.Count == 0");
            return default(ICommand);
        }
        return _queue.Dequeue()!.command;
    }

    public void Execute(GameObjectContext context)
    {
        if (_queue.Count == 0) return;
        CommandEntry? commandEntry = _queue.Peek()!;
        if (commandEntry.command.Execute(context) != PROCESS)
        {
            if (commandEntry.repeat.count >= 0 && commandEntry.repeat.PreDecrease(1) == 0)
            {
                _queue.Dequeue();
            }
        }
    }

}

public class StaticClock
{
    private bool _inf;
    private float _time;
    private float _timer;
    private Func<float, float> _timeFunc;

    public StaticClock(float time, Func<float, float> timeFunc, float timer = 0f)
    {
        _inf = false;
        _time = time;
        _timeFunc = timeFunc;
        _timer = timer;
    }
    public StaticClock()
    {
        _inf = true;
        _time = 0;
        _timeFunc = (param) => param;
        _timer = 0f;
    }

    public bool IsInfinite
    {
        get { return _inf; }
    }
    public float Time
    {
        get { return _time; }
    }
    public float Timer
    {
        get { return _timer; }
        set { _timer = value; }
    }
    public float ClockTimer
    {
        get { return _timeFunc(_timer); }
    }
    public float Rate
    {
        get
        {
            return ClockTimer / Time; 
        }
    }

    public void Tick(float tick)
    {
        _timer += tick;
    }

    public bool Check()
    {
        if (_inf) return false;
        return _time < _timeFunc(_timer);
    }

    public bool TickAndCheck(float tick)
    {
        Tick(tick);
        return Check();
    }
    public bool CheckAndTick(float tick)
    {
        var check = Check();
        Tick(tick);
        return check;
    }

    public void Reset()
    {
        _timer = 0f;
    }
}