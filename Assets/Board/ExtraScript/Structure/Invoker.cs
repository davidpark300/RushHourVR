using UnityEngine;
using System;
using System.Collections.Generic;

public class Invoker : MonoBehaviour
{

    private Dictionary<string, CommandQueue> _preCommand = new();
    private Dictionary<string, CommandQueue> _command = new();
    private Dictionary<string, CommandQueue> _postCommand = new();

    private GameObjectContext _context = null;
    public GameObjectContext Context { get { return _context; } }
    public bool IsInitialized { get; private set; } = false;

    void Start()
    {
        _context = new(gameObject);
        IsInitialized = true;
    }

    public void DoPre(string type, ICommand command, int repeat, int priority)
    {
        if (!_preCommand.ContainsKey(type))
        {
            _preCommand[type] = new();
        }
        _preCommand[type].Enqueue(command, repeat, priority);
    }
    public void Do(string type, ICommand command, int repeat, int priority)
    {
        if (!_command.ContainsKey(type))
        {
            _command[type] = new();
        }
        _command[type].Enqueue(command, repeat, priority);
    }
    public void DoPost(string type, ICommand command, int repeat, int priority)
    {
        if (!_postCommand.ContainsKey(type))
        {
            _postCommand[type] = new();
        }
        _postCommand[type].Enqueue(command, repeat, priority);
    }

    void Update()
    {
        foreach (var command in _preCommand)
        {
            command.Value.Execute(_context);
        }
        foreach (var command in _command)
        {
            command.Value.Execute(_context);
        }
        foreach (var command in _postCommand)
        {
            command.Value.Execute(_context);
        }
    }
}