using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class NIntent
{
    private Dictionary<object, object> _data = new Dictionary<object, object>();

    public void Push(object key, Int32 value)
    {
        if (!_data.ContainsKey(key))
            _data.Add(key, value);
        else
        {
            Debugger.Log("update NIntent key : "+ key + "; value : "+ value);
            _data[key] = value;
        }
    }

    public void PushDouble(object key, object value)
    {
        Push(key, value);
    }

    public void Push(object key, object value)
    {
        if (!_data.ContainsKey(key))
            _data.Add(key, value);
        else
        {
            Debugger.Log("update NIntent key : "+ key + "; value : {1}" + value);
            _data[key] = value;
        }
    }

    public void ChangeValue(object key, object value)
    {
        if (_data.ContainsKey(key))
            _data[key] = value;
        else
            Debugger.LogError("NIntent ChangeValue() error - key = " + key);
    }

    public bool Remove(object key)
    {
        return _data.Remove(key);
    }

    public object Value(object key)
    {
        return Value<object>(key);
    }

    public T Value<T>(object key)
    {
        if (_data.ContainsKey(key))
        {
            T res = (T)_data[key];
            return res;
        }
        else
        {
            return default(T);
        }
    }

    public bool HaveKey(object key)
    {
        return _data.ContainsKey(key);
    }

    public bool HaveValue(object value)
    {
        return _data.ContainsValue(value);
    }

    public void Clear()
    {
        _data.Clear();
    }

    public int Count()
    {
        return _data.Count;
    }

    public static NIntent Create()
    {
        return new NIntent();
    }
}
