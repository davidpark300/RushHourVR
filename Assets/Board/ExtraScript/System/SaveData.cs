using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

// ★ JSON 직렬화용: Unity 내장 JsonUtility는 Dictionary를 직접 못 다루므로
// 간단하게 POCO로 감싸서 직렬화/역직렬화 합니다.
[Serializable]
public class SaveModel
{
    public List<string> boolKeys = new();
    public List<bool> boolValues = new();

    public List<string> intKeys = new();
    public List<int> intValues = new();

    public List<string> floatKeys = new();
    public List<float> floatValues = new();

    public List<string> stringKeys = new();
    public List<string> stringValues = new();

    public int version = 1;
}

class InitializationSaveData
{
    private Dictionary<int, SaveModel> _data = new();
    public Dictionary<int, SaveModel> Data
    {
        get { return _data; }
        private set { _data = value; }
    }

    public InitializationSaveData()
    {
        _data[0] = new();
        _data[0].intKeys.Add(SaveData.INT_CHECKPOINT);
        _data[0].intValues.Add(0);

        _data[1] = new();
        _data[1].boolKeys.Add(SaveData.BOOL_B0_ELEVATOR_DETECTED);
        _data[1].boolValues.Add(false);
        _data[1].boolKeys.Add(SaveData.BOOL_B0_NOTE_DETECTED);
        _data[1].boolValues.Add(false);
        _data[1].boolKeys.Add(SaveData.BOOL_B0_PUZZLE_SOLVED);
        _data[1].boolValues.Add(false);
    }
}

public class SaveData : MonoBehaviour
{
    // 게임 데이터 키
    public static string INT_CHECKPOINT = "Checkpoint";
    public static string BOOL_B0_ELEVATOR_DETECTED = "B0ElevatorDetected";
    public static string BOOL_B0_NOTE_DETECTED = "B0NoteDetected";
    public static string BOOL_B0_PUZZLE_SOLVED = "B0PuzzleSolved";

    public static SaveData Instance { get; private set; }

    private InitializationSaveData _initializationData = new InitializationSaveData();

    // 런타임 메모리 상의 실제 저장소
    private readonly Dictionary<string, bool> _bools = new();
    private readonly Dictionary<string, int> _ints = new();
    private readonly Dictionary<string, float> _floats = new();
    private readonly Dictionary<string, string> _strings = new();

    private string _path;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _path = Path.Combine(Application.persistentDataPath, "savedata.json");
        GlobalLogger.Log("SaveData", "Awake", "Application.persistentDataPath", Application.persistentDataPath);
        Load(); // 없으면 새로 생성
    }

    // ======== Public API ========

    public bool HasKey(string key)
        => _bools.ContainsKey(key) || _ints.ContainsKey(key) || _floats.ContainsKey(key) || _strings.ContainsKey(key);

    public void RemoveKey(string key)
    {
        _bools.Remove(key);
        _ints.Remove(key);
        _floats.Remove(key);
        _strings.Remove(key);
        Save();
    }

    // 게임 데이터 초기화
    public void ResetSaveData()
    {
        var modelDict = _initializationData.Data;

        for (int cp = 0; cp < modelDict.Count; cp++)
        {
            var model = modelDict[cp];
            for (int i = 0; i < Math.Min(model.boolKeys.Count, model.boolValues.Count); i++)
                _bools[model.boolKeys[i]] = model.boolValues[i];

            for (int i = 0; i < Math.Min(model.intKeys.Count, model.intValues.Count); i++)
                _ints[model.intKeys[i]] = model.intValues[i];

            for (int i = 0; i < Math.Min(model.floatKeys.Count, model.floatValues.Count); i++)
                _floats[model.floatKeys[i]] = model.floatValues[i];

            for (int i = 0; i < Math.Min(model.stringKeys.Count, model.stringValues.Count); i++)
                _strings[model.stringKeys[i]] = model.stringValues[i];
        }

        Save();
    }

    // --- Bool ---
    public bool GetBool(string key, bool defaultValue = false)
        => _bools.TryGetValue(key, out var v) ? v : defaultValue;
    public void SetBool(string key, bool value, int checkpoint = 0)
    {
        _bools[key] = value;
        if (checkpoint != 0)
        {
            _ints[INT_CHECKPOINT] = checkpoint;
            Save();
        }
    }

    // --- Int ---
    public int GetInt(string key, int defaultValue = 0)
        => _ints.TryGetValue(key, out var v) ? v : defaultValue;
    public void SetInt(string key, int value, int checkpoint = 0)
    {
        _ints[key] = value;
        if (checkpoint != 0)
        {
            _ints[INT_CHECKPOINT] = checkpoint;
            Save();
        }
    }

    // --- Float ---
    public float GetFloat(string key, float defaultValue = 0f)
        => _floats.TryGetValue(key, out var v) ? v : defaultValue;
    public void SetFloat(string key, float value, int checkpoint = 0)
    {
        _floats[key] = value;
        if (checkpoint != 0)
        {
            _ints[INT_CHECKPOINT] = checkpoint;
            Save();
        }
    }

    // --- String ---
    public string GetString(string key, string defaultValue = "")
        => _strings.TryGetValue(key, out var v) ? v : defaultValue;
    public void SetString(string key, string value, int checkpoint = 0)
    {
        _strings[key] = value;
        if (checkpoint != 0)
        {
            _ints[INT_CHECKPOINT] = checkpoint;
            Save();
        }
    }

    // ======== Save / Load ========

    public void Save()
    {
        // POCO로 옮겨 담기
        var model = new SaveModel();
        foreach (var kv in _bools) { model.boolKeys.Add(kv.Key); model.boolValues.Add(kv.Value); }
        foreach (var kv in _ints) { model.intKeys.Add(kv.Key); model.intValues.Add(kv.Value); }
        foreach (var kv in _floats) { model.floatKeys.Add(kv.Key); model.floatValues.Add(kv.Value); }
        foreach (var kv in _strings) { model.stringKeys.Add(kv.Key); model.stringValues.Add(kv.Value); }

        var json = JsonUtility.ToJson(model, prettyPrint: true);

        // 원자적 저장: temp에 쓰고 교체
        var dir = Path.GetDirectoryName(_path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var temp = _path + ".tmp";
        File.WriteAllText(temp, json);
        if (File.Exists(_path)) File.Delete(_path);
        File.Move(temp, _path);
    }

    public void Load()
    {
        _bools.Clear(); _ints.Clear(); _floats.Clear(); _strings.Clear();

        if (!File.Exists(_path))
        {
            Save(); // 초기 파일 생성
            return;
        }

        try
        {
            var json = File.ReadAllText(_path);
            var model = JsonUtility.FromJson<SaveModel>(json);
            if (model == null) { Save(); return; }

            int checkpoint = model.intValues[0];

            for (int i = 0; i < Math.Min(model.boolKeys.Count, model.boolValues.Count); i++)
                _bools[model.boolKeys[i]] = model.boolValues[i];

            for (int i = 0; i < Math.Min(model.intKeys.Count, model.intValues.Count); i++)
                _ints[model.intKeys[i]] = model.intValues[i];

            for (int i = 0; i < Math.Min(model.floatKeys.Count, model.floatValues.Count); i++)
                _floats[model.floatKeys[i]] = model.floatValues[i];

            for (int i = 0; i < Math.Min(model.stringKeys.Count, model.stringValues.Count); i++)
                _strings[model.stringKeys[i]] = model.stringValues[i];
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SaveData Load failed, creating fresh file. Error: {e.Message}");
            Save(); // 손상 시 초기화
        }
    }

    void OnApplicationQuit()
    {
        Save();
    }
}