using UnityEngine;
using System;
using System.Collections.Generic;

public class SimpleInvoker : MonoBehaviour
{

    private CommandQueue _command = new();

    private GameObjectContext _context = null;
    public bool IsInitialized { get; private set; } = false;

    void Start()
    {
        _context = new(gameObject);
        IsInitialized = true;
    }

    public void Do(ICommand command, int repeat, int priority)
    {
        _command.Enqueue(command, repeat, priority);
    }

    void Update()
    {
        _command.Execute(_context);
    }
}