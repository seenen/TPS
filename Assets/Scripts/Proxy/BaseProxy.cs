using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PureMVC.Patterns.Proxy;

public class BaseProxy<T> : Proxy where T : BasePackage
{
    public new T Data {
        get
        {
            return (T)data;
        }
        set
        {
            data = value;
        }
    }
    public BaseProxy(string proxyName, T data = null) : base(proxyName, data)
    {
    }
}
