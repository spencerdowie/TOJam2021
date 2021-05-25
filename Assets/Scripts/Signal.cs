using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public static class GameSignals
{
    public static TypedSignal<bool> PauseGame = new TypedSignal<bool>("PauseGame");
    public static TypedSignal<LineViewCustom.NodeType> typedSignal = new TypedSignal<LineViewCustom.NodeType>("MatchType");
    public static Dictionary<string, Signal> NodeTypeSignals = new Dictionary<string, Signal>()
    {
        {"Work", new Signal("Work") }
    };
}

public class SignalParameters
{
    private Dictionary<string, object> parameters = new Dictionary<string, object>();

    public SignalParameters() { }

    public void AddParameter(string key, object value)
    {
        parameters[key] = value;
    }

    public void AddParameter(params KeyValuePair<string, object>[] pairs)
    {
        for(int i = 0; i < pairs.Length; ++i)
        {
            parameters[pairs[i].Key] = pairs[i].Value;
        }
    }

    public object GetParameter(string key)
    {
        return parameters[key];
    }

    public bool HasParameter(string key)
    {
        return parameters.ContainsKey(key);
    }

    public void ClearParameters()
    {
        parameters.Clear();
    }
}

public class Signal
{
    private readonly string name;

    public string Name { get => name; }

    public delegate void SignalListener();
    private List<SignalListener> listenerList = new List<SignalListener>();

    public Signal(string name)
    {
        this.name = name;
        listenerList = new List<SignalListener>();
    }

    public void AddListener(SignalListener listener)
    {
        listenerList.Add(listener);
    }

    public void RemoveListener(SignalListener listener)
    {
        listenerList.Remove(listener);
    }

    public void Dispatch()
    {
        if (listenerList.Count == 0)
            Debug.LogWarning($"There are no listeners to signal {name}.");
        for (int i = 0; i < listenerList.Count; ++i)
        {
            listenerList[i]();
        }
    }
}

public class TypedSignal<T>
{
    private readonly string name;

    public string Name { get => name; }
    public delegate void SignalListener(T parameter);
    private List<SignalListener> listeners;

    public TypedSignal(string name)
    {
        this.name = name;
        listeners = new List<SignalListener>();
    }

    public void AddListener(SignalListener listener)
    {
        Assert.IsTrue(!this.listeners.Contains(listener));
        this.listeners.Add(listener);
    }

    public void RemoveListener(SignalListener listener)
    {
        this.listeners.Remove(listener);
    }

    public void Dispatch(T parameter)
    {
        for(int i = 0; i < listeners.Count; ++i)
        {
            listeners[i](parameter);
        }
    }
}

public class TypedSignal<T1,T2>
{
    private readonly string name;

    public string Name { get => name; }
    public delegate void SignalListener(T1 p1, T2 p2);
    private List<SignalListener> listeners;

    public TypedSignal(string name)
    {
        this.name = name;
        listeners = new List<SignalListener>();
    }

    public void AddListener(SignalListener listener)
    {
        Assert.IsTrue(!this.listeners.Contains(listener));
        this.listeners.Add(listener);
    }

    public void RemoveListener(SignalListener listener)
    {
        this.listeners.Remove(listener);
    }

    public void Dispatch(T1 p1, T2 p2)
    {
        for (int i = 0; i < listeners.Count; ++i)
        {
            listeners[i](p1, p2);
        }
    }
}