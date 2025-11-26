using UnityEngine;
using System;
using System.Collections.Generic;

public static class GlobalConfig
{
    public static KeyConfig Key => _key ??= Resources.Load<KeyConfig>("KeyConfig");
    private static KeyConfig _key;
}

public static class GlobalLogger
{
    public static void Log(string objectName = "", string className = "", string methodName = "", string message = "")
    {
        Debug.Log("[L]" + objectName + " - " + className + " - " + methodName + " : " + message);
    }
    public static void Warning(string objectName, string className, string methodName, string message)
    {
        Debug.Log("[W]" + objectName + " - " + className + " - " + methodName + " : " + message);
    }
    public static void Error(string objectName, string className, string methodName, string message)
    {
        Debug.Log("[E]" + objectName + " - " + className + " - " + methodName + " : " + message);
    }
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }



    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}