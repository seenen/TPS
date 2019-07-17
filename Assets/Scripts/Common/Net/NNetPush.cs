using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NNetPush
{
    Dictionary<string, bool> m_pushIdMap = new Dictionary<string, bool>();

    NIntent intent = new NIntent();

    public void RegisterPush(string name, DelegatePushRPC rpc)
    {
        if (!intent.HaveKey(name))
            intent.Push(name, rpc);
        else
        {
            Debugger.Log("RegisterPush have same key : " + name + " ChangeValue!");
            intent.ChangeValue(name, rpc);
        }
    }

    public void RemovePush(string name)
    {
        if (intent.HaveKey(name))
            intent.Remove(name);
    }

    public void ClearPush()
    {
        intent.Clear();
    }

    public bool HaveKey(string name)
    {
        return intent.HaveKey(name);
    }

    ////主动抛网络事件，
    ////与下面的接口，有区别
    //public void PushNetEvent(string name, byte[] results)
    //{
    //    if (intent.HaveKey(name))
    //    {
    //        var callback = intent.Value<DelegatePushRPC>(name);
    //        if (callback != null)
    //        {
    //            callback(name, results);
    //        }
    //    }
    //}

    public void HandleNetPush(string name, byte[] results, ENetType type)
    {
        if (intent.HaveKey(name))
        {
            /// 返回给上层逻辑事件
            DelegatePushRPC d = intent.Value<DelegatePushRPC>(name);
            d(type, name, results);
        }
        else
        {
            Debugger.LogWarning("NNetPush! intent is not have key : " + name);
        }
    }
}