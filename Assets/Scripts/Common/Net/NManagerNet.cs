using Google.Protobuf;
using Net.ProtolJava;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public delegate void NetCallbackMessage(byte[] results);

public interface IHandleNetError
{
    /// <summary>
    /// 增加一个result接口，让其可以支持results数据
    /// </summary>
    /// <param name="error"></param>
    /// <param name="results"></param>
    /// <returns></returns>
    bool HandleNetError(int error, string name, ENetType type, byte[] results);
}
public enum ENetType
{
    Lobby = 0,
    Match = 1,
    Game = 2,
    Count = 3,
}
public class NManagerNet : MonoBehaviour
{
    IHandleNetError _handleErrorImpl = new NNetErrorImpl();
    public IHandleNetError HandleError { get { return _handleErrorImpl; } set { _handleErrorImpl = value; } }
    public class NetMessage
    {
        public NetCallbackMessage callback = null;
        public DelegateRPC rpcCallback = null;
        public ENetType netType = ENetType.Count;
        public List<byte[]> luaBuffer = null;
        public bool isLua = false;

        public string name;
        public IMessage args;
        public string token;

        public void NetCallback(int error, byte[] results)
        {
            int type = (int)netType;
            if (error == 0)
            {
                Callback(error, name, results);
                var messagelist = NManagerNet.Instance.m_pNetMessageList[type];
                messagelist.RemoveAt(0);
                if (messagelist.Count > 0)
                {
                    NManagerNet.Instance.RequestRpc(messagelist[0]);
                }
                else
                {
                    //NViewContainerBase.StopWait();
                }
            }
            else
            {
                Debugger.LogError(name + " NetCallback Error : " + error);
                Callback(error, name, results);
                //一旦发生ERROR就直接清理发送队列,状态重置
                NManagerNet.Instance.SetbSending(netType, false);
                //NViewContainerBase.StopWait();
                NManagerNet.Instance.m_pNetMessageList[type].Clear();
            }
        }

        void Callback(int error, string name, byte[] results)
        {
            bool handleError = NManagerNet.Instance.HandleError.HandleNetError(error, name, netType, results);
            if (!handleError)
            {
                if (callback != null && error == 0)
                {
                    callback(results);
                }
                else
                {
                    rpcCallback?.Invoke(error, results);
                }
            }
        }
    }

    public static NManagerNet Instance { get; private set; }
    TCPClient[] m_nets = new TCPClient[(int)ENetType.Count];
    string[] m_hosts = new string[(int)ENetType.Count];
    int[] m_ports = new int[(int)ENetType.Count];
    //LOBBY socket的超时时间记录
    private float[] m_SendTimeOut = new float[(int)ENetType.Count];
    private bool[] m_bSending = new bool[(int)ENetType.Count];
    public bool GetbSending(ENetType eNetType)
    {
        return m_bSending[(int)eNetType];
    }

    public void SetbSending(ENetType eNetType,bool flag)
    {
        m_bSending[(int)eNetType] = flag;
        m_SendTimeOut[(int)eNetType] = 8f;
    }

    public void SetIpAndPort(ENetType eNetType,string host,int port)
    {
        m_hosts[(int)eNetType] = host;
        m_ports[(int)eNetType] = port;
    }
    //缓存消息队列
    List<NetMessage>[] m_pNetMessageList = new List<NetMessage>[(int)ENetType.Count];
    private void Start()
    {
        Instance = this;
        Init();
        for(int i = 0; i < (int)ENetType.Count; i++)
        {
            m_SendTimeOut[i] = 8f;
            m_bSending[i] = false;
        }
    }
    Thread m_threadHeartBeat;
    public void Init()
    {
        //开启线程每5秒发送一次心跳包
        if (m_threadHeartBeat != null)
            m_threadHeartBeat.Abort();
        m_threadHeartBeat = new Thread(new ThreadStart(HeartBeatUpdate));
        m_threadHeartBeat.Start();

        handlePush = new NNetPush[(int)ENetType.Count];
        for(int i = 0; i < (int)ENetType.Count; i++)
        {
            handlePush[i] = new NNetPush();
        }
    }
    public void Destroy()
    {
        if (m_threadHeartBeat != null)
            m_threadHeartBeat.Abort();
    }

    public void ApplicationPause(bool paused)
    {
        IsBackGround = paused;
    }

    public void InitNetByType(ENetType eNetType)
    {
        if (m_pNetMessageList[(int)eNetType] == null)
            m_pNetMessageList[(int)eNetType] = new List<NetMessage>();

        TCPClient rpc = GetRpcNet(eNetType);
        if (rpc == null)
        {
            rpc = new TCPClient();
            SetRpcNet(eNetType, rpc);
        }
        //开始连接
        rpc.SendConnect(m_hosts[(int)eNetType], m_ports[(int)eNetType], eNetType);
        rpc.InitPushCallBack(NetPush);
        rpc.InitExceptionCallBack(NetException);

        SetbSending(eNetType,false);
    }
    private void RequestRpc(NetMessage message)
    {
        //NViewContainerBase.StartWait();
        SetbSending(message.netType, true);
        if (message.isLua == false)
            m_nets[(int)message.netType].RequestRpc(message.name, message.NetCallback, message.args);
    }
    public TCPClient GetRpcNet(ENetType netType)
    {
        return m_nets[(int)netType];
    }
    public void SetRpcNet(ENetType netType, TCPClient network)
    {
        m_nets[(int)netType] = network;
    }

    //HANDLE底层异常
    void NetException(ENetType eNetType, string name)
    {
        if (name.Equals(DisType.ConnectFailed.ToString()) || name.Equals(DisType.OnConnectFailed.ToString()) || name.Equals(DisType.Disconnect.ToString()))
        {
            //断网异常
            //NViewContainerBase.StopWait();
            SetbSending(eNetType,false);
            var messagelist = m_pNetMessageList[(int)eNetType];
            if (messagelist.Count > 0)
            {
                if (m_nets[(int)eNetType] == null || !m_nets[(int)eNetType].IsConnect())
                {
                    InitNetByType(eNetType);
                }
                RequestRpc(messagelist[0]);
            }
        }
        else if (name.Equals(DisType.ReceivedError.ToString()))
        {
            //解析失败
            m_pNetMessageList[(int)eNetType].Clear();
            //NViewContainerBase.StopWait();
            SetbSending(eNetType,false);
            Close(eNetType);
        }
        else if (name.Equals(DisType.SendRequestFailed.ToString()))
        {
            //发送失败异常
            var messagelist = m_pNetMessageList[(int)eNetType];
            if (messagelist.Count > 0)
            {
                if (m_nets[(int)eNetType] == null || !m_nets[(int)eNetType].IsConnect())
                {
                    InitNetByType(eNetType);
                }
                RequestRpc(messagelist[0]);
            }
        }
        else if (name.Equals(DisType.Disconnect.ToString()))
        {
            Debugger.LogWarning("NetLobbyException name.Equals(DisType.Disconnect.ToString()");
            //连接失败异常
            m_pNetMessageList[(int)eNetType].Clear();
            //NViewContainerBase.StopWait();
            SetbSending(eNetType, false);
            Close(eNetType);
        }
    }
    //关闭网络
    public void Close(ENetType netType)
    {
        var rpc = GetRpcNet(netType);
        if (rpc != null)
        {
            rpc.OnDisconnected(string.Format("NManagerNet Close ENetType = {0}", netType.ToString()));
        }
    }

    public void SendMessage(ENetType eNetType, string name, NetCallbackMessage callback, IMessage args)
    {
        if (args == null)
        {
            Debugger.LogError("Message "+ name + " data is null");
            return;
        }
        if (m_nets[(int)eNetType] == null || !m_nets[(int)eNetType].IsConnect())
        {
            InitNetByType(eNetType);
        }

        NetMessage message = new NetMessage();
        message.callback = delegate (byte[] result)
        {
            //NViewContainerBase.StopWait();
            SetbSending(eNetType,false);
            callback(result);
        };
        message.name = name;
        message.netType = eNetType;
        message.args = args;
        message.token = System.Guid.NewGuid().ToString();
        if (m_pNetMessageList[(int)eNetType].Count == 0)
        {
            //NViewContainerBase.StartWait();
            SetbSending(eNetType,true);
            m_nets[(int)eNetType].RequestRpc(message.name, message.NetCallback, message.args);
        }
        m_pNetMessageList[(int)eNetType].Add(message);
        
    }
    public void SendMessage(ENetType eNetType, string name, DelegateRPC callback, IMessage args)
    {
        if (args == null)
        {
            Debugger.LogError("Message "+name+" data is null");
            return;
        }
        if (m_nets[(int)eNetType] == null || !m_nets[(int)eNetType].IsConnect())
        {
            InitNetByType(eNetType);
        }
        NetMessage message = new NetMessage();
        message.rpcCallback = delegate (int error, byte[] result)
        {
            //NViewContainerBase.StopWait();
            SetbSending(eNetType,false);
            callback(error, result);
        };
        message.name = name;
        message.netType = eNetType;
        message.args = args;
        message.token = System.Guid.NewGuid().ToString();
        if (m_pNetMessageList[(int)eNetType].Count == 0)
        {
            //NViewContainerBase.StartWait();
            SetbSending(eNetType,true);
            m_nets[(int)eNetType].RequestRpc(message.name, message.NetCallback, message.args);
        }
        m_pNetMessageList[(int)eNetType].Add(message);
    }

    //Push管理
    NNetPush[] handlePush = null;
    //HANDLE所有LOBBY相关的PUSH消息
    void NetPush(ENetType eNetType, string name, byte[] results)
    {
        handlePush[(int)eNetType].HandleNetPush(name, results, eNetType);
    }

    public void Update()
    {
        for (int i = 0; i < (int)ENetType.Count; ++i)
        {
            if (m_nets[i] != null)
            {
                m_nets[i].Update(Time.deltaTime);
            }
        }

        //提示网络不给力
        for (int i = 0; i < (int)ENetType.Count; i++)
        {
            if (m_nets[i] != null && GetbSending((ENetType)i))
            {
                m_SendTimeOut[i] = m_SendTimeOut[i] - Time.deltaTime;
                if (m_SendTimeOut[i] <= 0f)
                {
                    m_SendTimeOut[i] = 8f;
                    //NViewContainerBase.Instance.OpenTips(2115);
                    NetException((ENetType)i,DisType.SendRequestFailed.ToString());
                }
            }
        }
    }

    public void RegisterPush(string name, DelegatePushRPC rpc, ENetType netType)
    {
        handlePush[(int)netType].RegisterPush(name, rpc);
    }

    public void RemovePush(string name, ENetType netType)
    {
        handlePush[(int)netType].RemovePush(name);
    }
    
    public void CleanAllPush()
    {
        for (int i = 0; i < handlePush.Length; ++i)
        {
            handlePush[i].ClearPush();
        }
    }

    //清理单个Socket注册的PUSH事件
    public void CleanPush(ENetType type)
    {
        handlePush[(int)type].ClearPush();
    }

    [HideInInspector]
    public bool IsBackGround = false;

    void HeartBeatUpdate()
    {
        while (true)
        {
            if (!IsBackGround)
            {
                for(int i = 0; i < (int)ENetType.Count;i++)
                {
                    if (m_nets[i] != null && m_nets[i].IsConnect())
                    {
                        m_nets[i].RequestHeartBeat();
                    }
                }
            }
            Thread.Sleep(5000);
        }
    }

    private void OnApplicationQuit()
    {
        for (int i = 0; i < (int)ENetType.Count; i++)
        {
            if (m_nets[i] != null && m_nets[i].IsConnect())
            {
                m_nets[i].OnDisconnected("");
            }
        }
    }
}
