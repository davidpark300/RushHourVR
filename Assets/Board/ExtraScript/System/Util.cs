#nullable enable

using UnityEngine;
using System;
using System.Collections.Generic;

public class Util
{

    public static bool ContainAttribute(GameObject go, string attribute)
    {
        Attribute[] goAttributes = go.GetComponents<Attribute>();
        if (goAttributes == null) return false;
        foreach (Attribute goAttribute in goAttributes)
        {
            if (goAttribute.attribute == attribute) return true;
        }
        return false;
    }

    public static TComponent? GetOnlyComponent<TComponent>(GameObject go) where TComponent : Component
    {
        TComponent[] components = go.GetComponents<TComponent>();
        if (components == null || components.Length != 1)
        {
            GlobalLogger.Error(go.name, nameof(Util), nameof(GetOnlyComponent), "components == null || components.Length != 1");
            return null;
        }
        return components[0];
    }

    public static GameObject[] FindChildrenWithAttribute(GameObject parent, string attribute)
    {
        Transform parentTransform = parent.transform;
        List<GameObject> targetChildren = new List<GameObject>();
        foreach (Transform child in parentTransform.GetComponentsInChildren<Transform>())
        {
            if (ContainAttribute(child.gameObject, attribute) && child != parentTransform)
            {
                targetChildren.Add(child.gameObject);
            }
        }
        return targetChildren.ToArray();
    }
    public static GameObject? FindChildWithAttribute(GameObject parent, string attribute)
    {
        GameObject[] targetChildren = FindChildrenWithAttribute(parent, attribute);
        if (targetChildren == null)
        {
            GlobalLogger.Error(parent.name, nameof(Util), nameof(FindOnlyChildWithAttribute), "targetChildren == null");
            return null;
        }
        return targetChildren[0];
    }
    public static GameObject? FindOnlyChildWithAttribute(GameObject parent, string attribute)
    {
        GameObject[] targetChildren = FindChildrenWithAttribute(parent, attribute);
        if (targetChildren == null || targetChildren.Length != 1)
        {
            GlobalLogger.Error(parent.name, nameof(Util), nameof(FindOnlyChildWithAttribute), "targetChildren == null || targetChildren.Length != 1");
            return null;
        }
        return targetChildren[0];
    }
    public static TComponent? FindOnlyComponentInOnlyChildWithAtrribute<TComponent>(GameObject parent, string attribute) where TComponent : Component
    {
        GameObject? targetChild = FindOnlyChildWithAttribute(parent, attribute);
        if (targetChild == null)
        {
            GlobalLogger.Error(parent.name, nameof(Util), nameof(FindOnlyComponentInOnlyChildWithAtrribute), "targetChild == null");
            return null;
        }
        TComponent[] components = targetChild.GetComponents<TComponent>();
        if (components == null || components.Length != 1)
        {
            GlobalLogger.Error(targetChild.name, nameof(Util), nameof(FindOnlyComponentInOnlyChildWithAtrribute), "components == null || components.Length != 1");
            return null;
        }
        return components[0];
    }

    public static GameObject[] FindChildrenWithTag(GameObject parent, string tag)
    {
        Transform parentTransform = parent.transform;
        List<GameObject> targetChildren = new List<GameObject>();
        foreach (Transform child in parentTransform.GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag(tag) && child != parentTransform)
            {
                targetChildren.Add(child.gameObject);
            }
        }
        return targetChildren.ToArray();
    }
    public static GameObject? FindOnlyChildWithTag(GameObject parent, string tag)
    {
        GameObject[] targetChildren = FindChildrenWithTag(parent, tag);
        if (targetChildren == null || targetChildren.Length != 1)
        {
            GlobalLogger.Error(parent.name, nameof(Util), nameof(FindOnlyChildWithTag), "targetChildren == null || targetChildren.Length != 1");
            return null;
        }
        return targetChildren[0];
    }
    public static TComponent? FindOnlyComponentInOnlyChildWithTag<TComponent>(GameObject parent, string tag) where TComponent : Component
    {
        GameObject? targetChild = FindOnlyChildWithTag(parent, tag);
        if (targetChild == null)
        {
            GlobalLogger.Error(parent.name, nameof(Util), nameof(FindOnlyComponentInOnlyChildWithTag), "targetChild == null");
            return null;
        }
        TComponent[] components = targetChild.GetComponents<TComponent>();
        if (components == null || components.Length != 1)
        {
            GlobalLogger.Error(targetChild.name, nameof(Util), nameof(FindOnlyComponentInOnlyChildWithTag), "components == null || components.Length != 1");
            return null;
        }
        return components[0];
    }

    public static GameObject[] FindChildrenWithName(GameObject parent, string name)
    {
        Transform parentTransform = parent.transform;
        List<GameObject> targetChildren = new List<GameObject>();
        foreach (Transform child in parentTransform.GetComponentsInChildren<Transform>())
        {
            if (child.gameObject.name == name && child != parentTransform)
            {
                targetChildren.Add(child.gameObject);
            }
        }
        return targetChildren.ToArray();
    }

    public static void SetOrthoAspect(Camera camera, float aspect)
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / aspect;

        if (scaleHeight < 1.0f)
        {
            Rect rect = camera.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            camera.rect = rect;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = camera.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            camera.rect = rect;
        }
    }
    public static void SetOrthoSize(Camera camera, float size)
    {
        camera.orthographicSize = size;
    }
}

public class GameObjectContext
{
    public GameObject gameObject;
    protected Dictionary<Type, Component[]> _cache;
    protected Dictionary<string, object?> _extra = new(StringComparer.Ordinal);

    public GameObjectContext(GameObject go)
    {
        gameObject = go;
        _cache = new();
    }

    public T? GetComponent<T>() where T : Component
    {
        if (_cache.TryGetValue(typeof(T), out var comp))
        {
            return (T)(comp[0]);
        }

        var c = gameObject.GetComponents<T>();
        _cache[typeof(T)] = c;
        if (c == null) return default(T);
        return (T)(c[0]);
    }
    public T[] GetComponents<T>() where T : Component
    {
        if (_cache.TryGetValue(typeof(T), out var comp))
        {
            return (T[])comp;
        }

        var c = gameObject.GetComponents<T>();
        _cache[typeof(T)] = c;
        return c;
    }
    public T? GetOnlyComponent<T>() where T : Component
    {
        T[] components = GetComponents<T>();
        if (components == null || components.Length != 1)
        {
            GlobalLogger.Error(gameObject.name, nameof(GameObjectContext), nameof(GetOnlyComponent), "T == null || T.Length != 1");
            return null;
        }
        return components[0];
    }

    public void SetExtra<T>(string key, T value) => _extra[key] = value;
    public bool TryGetExtraData<T>(string key, out T value)
    {
        if (!_extra.TryGetValue(key, out var obj))
        {
            value = default!;
            return false;
        }

        // null 처리
        if (obj is null)
        {
            // 참조형/nullable 값형이면 default(T)로 성공 처리
            if (default(T) is null)
            {
                value = default!;
                return true;
            }
            value = default!;
            return false;
        }

        // 이미 T면 바로 반환
        if (obj is T t)
        {
            value = t;
            return true;
        }
        value = default!;
        return false;
    }

    public GameObject[] FindChildrenWithAttribute(string attribute)
    {
        return Util.FindChildrenWithAttribute(gameObject, attribute);
    }

    public GameObject[] FindChildrenWithTag(string tag)
    {
        return Util.FindChildrenWithTag(gameObject, tag);
    }

    public GameObject[] FindChildrenWithName(string name)
    {
        return Util.FindChildrenWithName(gameObject, name);
    }

}

public class Token
{
    private HashSet<string> _tokens = new();
    private Dictionary<string, bool> _changedTokens = new();

    public bool Set(string key)
    {
        if (_tokens.Contains(key))
        {
            _changedTokens.Remove(key);
            return false;
        }
        _changedTokens[key] = true;
        _tokens.Add(key);
        return true;
    }

    public bool Release(string key)
    {
        if (_tokens.Contains(key) == false)
        {
            _changedTokens.Remove(key);
            return false;
        }
        _changedTokens[key] = false;
        _tokens.Remove(key);
        return true;
    }

    public bool Get(string key)
    {
        return _tokens.Contains(key);
    }

    public bool Changed(string key, bool pre)
    {
        if (_changedTokens.ContainsKey(key) == false) return false;
        bool changed = _changedTokens[key];
        _changedTokens.Remove(key);
        return changed == pre;
    }
}
