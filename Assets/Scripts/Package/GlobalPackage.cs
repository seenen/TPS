using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class GlobalPackage : BasePackage
{
    public string uuid;
    public string roomId;
    public string ip;
    public int port;

    public string udpIp;
    public int udpPort;

    public override void Init(params object[] args)
    {
        base.Init(args);
    }

    public override void Release(params object[] args)
    {
        base.Release(args);
    }
}