using Google.Protobuf;
using Net.ProtolJava;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public enum DisType
{
    ConnectFailed,              //连接失败（客户端问题为主）
    OnConnectFailed,            //无法连接（服务器问题为主）
    SendRequestFailed,          //发送失败
    Disconnect,                 //断开（各种原因连接后又断开连接了）
    ReceivedError,              //收到错误的包，无法解析
}

public enum ExceptionType
{
    None,
    Push,                       //走PUSH流程抛异常
    Exception,                  //走EXCEPTION流程抛异常
}

public class NException
{
    public string m_exceptionName = "";
    public ExceptionType m_exceptionType = ExceptionType.None;
}

public delegate void DelegateRPC(int error, byte[] results);
public delegate void DelegateExceptionRPC(ENetType eNetType, string name);
public delegate void DelegatePushRPC(ENetType eNetType, string name, byte[] results);

public class TCPClient
{
    private TcpClient _tcpclient = null;
    private NetworkStream outStream = null;//网络输出流
    private MemoryStream memStream;
    private BinaryReader reader;
    private const int MAX_READ = 14 * 1024;//14K
    private byte[] byteBuffer = new byte[MAX_READ];
    private byte[] _receivePacket = null;//服务器下发的包
    private bool IsConnectionSuccessful = false;//是否连接成功
    private bool IsBeginConnect = false;//是否开始连接

    // 头内容
    int _receiveHeadSize;//包头大小

    Queue<byte[]> _requestPackets = new Queue<byte[]>(); //存储的发包数据
    List<byte[]> _responsePackets = new List<byte[]>(); //存储的收包数据
    Dictionary<string, DelegateRPC> _responseCallBack = new Dictionary<string, DelegateRPC>(); //存储回包后的callback

    //Debug用
    List<NException> _listExceptions = new List<NException>();
    List<string> _listLogs = new List<string>();

    ENetType m_eNetType = ENetType.Count;

    /// <summary>
    /// 建立TCP连接
    /// </summary>
    /// <param name="host">IP</param>
    /// <param name="port">Port</param>
    public void SendConnect(string host, int port , ENetType eNetType)
    {
        if (IsBeginConnect)
            return;
        IsConnectionSuccessful = false;
        m_eNetType = eNetType;
        IPAddress[] addr = Dns.GetHostAddresses(host);
        if (addr.Length == 0)
        {
            OnExceptions(DisType.OnConnectFailed, ExceptionType.Exception);
            return;
        }
        _tcpclient = new TcpClient(addr[0].AddressFamily);
        memStream = new MemoryStream();
        reader = new BinaryReader(memStream);

        try
        {
            OnLog("SendConnect host = " + host + " port = " + port);
            IsBeginConnect = true;
            _tcpclient.BeginConnect(addr[0], port, new AsyncCallback(OnConnected), null);
        }
        catch (Exception e)
        {
            string str = string.Format("<Net> SendConnect Error! {0}", e.Message);
            OnDisconnected(str);
            OnExceptions(DisType.ConnectFailed, ExceptionType.Exception);
        }
    }
    /// <summary>
    /// 连接成功
    /// </summary>
    /// <param name="asr"></param>
    protected void OnConnected(IAsyncResult asr)
    {
        IsBeginConnect = false;

        if (_tcpclient != null && _tcpclient.Connected)
        {
            //连接成功后，结束挂起的异步连接尝试
            _tcpclient.EndConnect(asr);
            OnLog("OnConnected Finish 连接成功!");

            IsConnectionSuccessful = true;
            outStream = _tcpclient.GetStream();
            outStream.BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), null);
        }
        else
        {
            string str = "<Net> OnConnectFailed! _tcpclient is null or !_tcpclient.Connected";
            OnDisconnected(str);
            OnExceptions(DisType.OnConnectFailed, ExceptionType.Exception);
        }
    }
    /// <summary>
    /// 断开连接
    /// </summary>
    /// <param name="msg">断开信息</param>
    public void OnDisconnected(string msg)
    {
        if (reader != null)
            reader.Close();

        if (memStream != null)
        {
            memStream.Dispose();
            memStream.Close();
        }

        if (outStream != null)
        {
            outStream.Dispose();
            outStream.Close();
        }

        if (_tcpclient != null)
        {
            try
            {
                _tcpclient.Close();
            }
            catch (Exception e)
            {
                Debugger.LogError("_tcpclient.Close() is error !!!  " + e.Message);
            }
        }

        _tcpclient = null;

        _responseCallBack.Clear();
        _responsePackets.Clear();
        _receivePacket = null;
        _requestPackets.Clear();

        _receiveHeadSize = 0;
        Array.Clear(byteBuffer, 0, byteBuffer.Length);

        IsConnectionSuccessful = false;

        OnLog(msg);
    }

    public void Update(double deltaTime = 0.017)
    {
        lock (this)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable && IsConnectionSuccessful)
                OnExceptions(DisType.Disconnect, ExceptionType.Exception);

            for (int i = 0; i < _listExceptions.Count; ++i)
            {
                if (_listExceptions[i].m_exceptionType.Equals(ExceptionType.Push))
                {
                    ResponsePush(_listExceptions[i].m_exceptionName, null);
                }
                else
                    ResponseException(_listExceptions[i].m_exceptionName);
            }
            _listExceptions.Clear();

            //发送一次消息
            SendRequests();

            if (_responsePackets.Count > 0)
            {
                var packet = _responsePackets[0];
                _responsePackets.RemoveAt(0);
                OnPackedReceived(packet);
            }

            for (int i = 0; i < _listLogs.Count; ++i)
            {
                Debugger.LogWarning(_listLogs[i]);
            }
            _listLogs.Clear();
        }
    }

    void OnRead(IAsyncResult asr)
    {
        try
        {
            int bytesRead = 0;
            //读取字节流到缓冲区
            bytesRead = outStream.EndRead(asr);
            if (bytesRead < 1)
            {
                OnDisconnected("<Net> OnDisconnected! bytesRead = 0");
                OnExceptions(DisType.Disconnect, ExceptionType.Exception);
                return;
            }
            else
            {
                //分析数据包内容，抛给逻辑层
                OnReceive(byteBuffer, bytesRead);
            }
            //分析完，再次监听服务器发过来的新消息
            //清空数组 
            Array.Clear(byteBuffer, 0, byteBuffer.Length);
            outStream.BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), null);
        }
        catch (Exception e)
        {
            string str = string.Format("<Net> OnRead Exception! {0}", e.Message);
            OnDisconnected(str);
            OnExceptions(DisType.Disconnect, ExceptionType.Exception);
            return;
        }
    }
    /// <summary>
    /// 接收到消息
    /// </summary>
    void OnReceive(byte[] bytes, int length)
    {
        //在尾巴添加新的数据
        memStream.Seek(0, SeekOrigin.End);
        memStream.Write(bytes, 0, length);
        //添加完成后，回到头开始读取
        memStream.Seek(0, SeekOrigin.Begin);

        int headByteCount = 4;
        if (RemainingBytes() >= headByteCount)
        {
            byte[] headSize = reader.ReadBytes(headByteCount);
            Array.Reverse(headSize);
            _receiveHeadSize = BitConverter.ToInt32(headSize, 0);
            if(RemainingBytes() >= _receiveHeadSize)
            {
                byte[] headBytes = reader.ReadBytes(_receiveHeadSize);
                TcpPacketHead fpsTcpPacketHead = TcpPacketHead.Parser.ParseFrom(headBytes);
                int dataSize = fpsTcpPacketHead.PacketSize;
                if (RemainingBytes() >= dataSize)
                {
                    if (dataSize > 0)
                    {
                        byte[] dataBytes = reader.ReadBytes(dataSize);
                        byte[] buffer = new byte[headByteCount + _receiveHeadSize + dataSize];
                        Array.ConstrainedCopy(headSize, 0, buffer, 0, headByteCount);//FpsTcpPacketHead大小4字节
                        Array.ConstrainedCopy(headBytes, 0, buffer, headByteCount, _receiveHeadSize);//FpsTcpPacketHead
                        Array.ConstrainedCopy(dataBytes, 0, buffer, headByteCount + _receiveHeadSize, dataSize);
                        _responsePackets.Add(buffer);
                    }
                    else
                    {
                        byte[] buffer = new byte[headByteCount + _receiveHeadSize];
                        Array.ConstrainedCopy(headSize, 0, buffer, 0, headByteCount);//FpsTcpPacketHead大小4字节
                        Array.ConstrainedCopy(headBytes, 0, buffer, headByteCount, _receiveHeadSize);//FpsTcpPacketHead
                        _responsePackets.Add(buffer);
                    }
                }
                else
                {
                    memStream.Position = memStream.Position - headByteCount - _receiveHeadSize;
                }
            }
            else
            {
                memStream.Position = memStream.Position - headByteCount;
            }
        }

        byte[] leftover = reader.ReadBytes((int)RemainingBytes());
        memStream.SetLength(0);
        memStream.Write(leftover, 0, leftover.Length);
    }

    /// <summary>
    /// 剩余的字节
    /// </summary>
    long RemainingBytes()
    {
        return memStream.Length - memStream.Position;
    }
    
    /// <summary>
    /// 初始化异常的回调，主要是给net层和底层做异常通信的
    /// </summary>
    DelegateExceptionRPC exceptionrpc = null;
    public void InitExceptionCallBack(DelegateExceptionRPC rpc)
    {
        exceptionrpc = rpc;
    }

    /// <summary>
    /// 底层异常的callback
    /// </summary>
    protected void ResponseException(string name)
    {
        if (exceptionrpc != null)
        {
            try
            {
                exceptionrpc(m_eNetType, name);
            }
            catch (Exception e)
            {
                string str = string.Format("<Net>ResponseException Net: exception caught in callback: {0}, trace : {1}", e.Message, e.StackTrace);
                OnLog(str);
            }
        }
    }

    /// <summary>
    /// 发送数据
    /// </summary>
    void SendRequests()
    {
        if (_tcpclient == null || !_tcpclient.Connected)
        {
            return;
        }
        if (outStream == null || !outStream.CanWrite)
        {
            return;
        }
        if (_requestPackets == null || _requestPackets.Count == 0)
        {
            return;
        }

        MemoryStream ms = null;
        using (ms = new MemoryStream())
        {
            byte[] req = _requestPackets.Dequeue();

            ms.Position = 0;
            BinaryWriter writer = new BinaryWriter(ms);
            // 写入所有的BUFFER
            writer.Write(req);
            writer.Flush();
            byte[] payload = ms.ToArray();
            outStream.BeginWrite(payload, 0, payload.Length, new AsyncCallback(OnWrite), null);
        }
    }

    /// <summary>
    /// 解析消息（主线程）
    /// </summary>
    protected void OnPackedReceived(byte[] packet)
    {
        TcpPacketHead header = null;
        try
        {
            byte[] headSizeBytes = new byte[4];
            headSizeBytes[0] = packet[0];
            headSizeBytes[1] = packet[1];
            headSizeBytes[2] = packet[2];
            headSizeBytes[3] = packet[3];
            int headInfoSize = BitConverter.ToInt32(headSizeBytes, 0);
            byte[] FpsTcpPacketHeadBytes = packet.Skip(4).Take(headInfoSize).ToArray();
            header = TcpPacketHead.Parser.ParseFrom(FpsTcpPacketHeadBytes);
            if (header.RpcName == "HeartBeat")
            {
                //Debugger.Log("HeartBeat");
            }
            else if (header.RpcName.Contains("Push"))
            {
                byte[] dataBytes = packet.Skip(4 + headInfoSize).Take(packet.Length - 4 - headInfoSize).ToArray();
                //push 的消息
                ResponsePush(header.RpcName, dataBytes);
            }
            //OnLog("<Net> 收到消息 TOKEN = " + header.RequestToken + " Error = " + header.Error);
            else if (_responseCallBack.ContainsKey(header.RpcName))
            {
                byte[] dataBytes = packet.Skip(4 + headInfoSize).Take(packet.Length - 4 - headInfoSize).ToArray();
                ResponseCallback(header.RpcName, 0, _responseCallBack[header.RpcName], dataBytes);
                _responseCallBack.Remove(header.RpcName);
            }
            else
            {
                OnLog("<Net> OnPackedReceived Error! Pack Isnot Impl ...");
            }
        }
        catch (Exception e)
        {
            OnLog(string.Format("<Net> OnPackedReceived Error! e.Message = {0}", e.Message));
            OnExceptions(DisType.ReceivedError, ExceptionType.Exception);
        }
    }
    /// <summary>
    /// 向链接写入数据流
    /// </summary>
    void OnWrite(IAsyncResult r)
    {
        try
        {
            outStream.EndWrite(r);
        }
        catch (Exception e)
        {
            // 发送失败抛出异常,不作处理（#） 
            string str = string.Format("<Net> SendRequests OnWrite Error! {0}", e.Message);
            OnLog(str);
            OnExceptions(DisType.SendRequestFailed, ExceptionType.Exception);
        }
    }
    /// <summary>
    /// 回调response的callback
    /// </summary>
    protected void ResponseCallback(string name, int error, DelegateRPC callback, byte[] packet)
    {
        // 回调上层
        if (callback != null)
        {
            try
            {
                callback(error, packet);
            }
            catch (Exception e)
            {
                string str = string.Format("<Net>ResponseCallback Net: exception caught in callback: {0}, trace : {1}", e.Message, e.StackTrace);
                OnLog(str);
            }
        }
    }
    public bool IsConnect()
    {
        return (_tcpclient != null && _tcpclient.Connected);
    }

    /// <summary>
    /// 发送RPC
    /// </summary>
    public void RequestRpc(string requestname, DelegateRPC callback, IMessage args)
    {
        byte[] bytes = args.ToByteArray();
        RequestRpc(requestname, callback, bytes);
    }

    /// <summary>
    /// 发送RPC2
    /// </summary>
    public void RequestRpc(string requestname, DelegateRPC callback, byte[] args)
    {
        TcpPacketHead header = new TcpPacketHead();
        header.RpcName = requestname;
        header.PacketSize = args.Length;
        int headSize = header.CalculateSize();
        //前4个字节代表FpsTcpPacketHead 包头长度
        byte[] buffer = new byte[args.Length + headSize + 4];
        //FpsUdpPacketHead 包头长度
        byte[] headSizeBytes = BitConverter.GetBytes(headSize);

        //FpsUdpPacketHead 字节数组
        byte[] headBytes = new byte[headSize];
        var stream = new CodedOutputStream(headBytes);
        header.WriteTo(stream);

        Array.Reverse(headSizeBytes);//由于c#和Java字节序不同，需要反一下，其他的proto做过处理所以不用反
        Array.ConstrainedCopy(headSizeBytes, 0, buffer, 0, headSizeBytes.Length);//FpsTcpPacketHead大小2字节
        Array.ConstrainedCopy(headBytes, 0, buffer, 4, headSize);//FpsTcpPacketHead
        Array.ConstrainedCopy(args, 0, buffer, 4 + headSize, args.Length);

        if (!_responseCallBack.ContainsKey(requestname))
            _responseCallBack.Add(requestname, callback);
        
        _requestPackets.Enqueue(buffer);
    }
    /// <summary>
    /// 发送HEARTBEAT
    /// </summary>
    /// <param name="requestname"></param>
    public void RequestHeartBeat()
    {
        if (_tcpclient == null || !_tcpclient.Connected)
        {
            return;
        }
        if (outStream == null || !outStream.CanWrite)
        {
            return;
        }
        TcpPacketHead fpsTcpPacketHead = new TcpPacketHead();
        fpsTcpPacketHead.RpcName = "HeartBeat";
        fpsTcpPacketHead.PacketSize = 0;

        int headSize = fpsTcpPacketHead.CalculateSize();
        byte[] headBytes = new byte[headSize];
        var stream = new CodedOutputStream(headBytes);
        fpsTcpPacketHead.WriteTo(stream);
        byte[] headSizeBytes = BitConverter.GetBytes(headSize);
        Array.Reverse(headSizeBytes);//由于c#和Java字节序不同，需要反一下，其他的proto做过处理所以不用反
        byte[] buffer = new byte[headSize + 4];
        Array.ConstrainedCopy(headSizeBytes, 0, buffer, 0, headSizeBytes.Length);//FpsTcpPacketHead大小4字节
        Array.ConstrainedCopy(headBytes, 0, buffer, headSizeBytes.Length, headSize);//FpsTcpPacketHead
        outStream.BeginWrite(buffer, 0, buffer.Length, new AsyncCallback(OnWrite), null);
    }

    /// <summary>
    /// 初始化PUSH的回调
    /// </summary>
    DelegatePushRPC pushrpc = null;
    public void InitPushCallBack(DelegatePushRPC rpc)
    {
        pushrpc = rpc;
    }

    // <summary>
    // 回调push的callback
    // </summary>
    protected void ResponsePush(string name, byte[] packet)
    {
        if (packet == null)
        {
            OnLog("<Net>ResponsePush Net : Packet is null!");
        }
        if (pushrpc != null)
        {
            try
            {
                pushrpc(m_eNetType, name, packet);
            }
            catch (Exception e)
            {
                string str = string.Format("<Net>ResponsePush Net: exception caught in callback: {0}, trace : {1}", e.Message, e.StackTrace);
                OnLog(str);
            }
        }
    }

    /// <summary>
    /// 写日志
    /// </summary>
    /// <param name="msg"></param>
    void OnLog(string msg)
    {
        _listLogs.Add(msg);
    }

    /// <summary>
    /// 记录异常
    /// </summary>
    /// <param name="msg"></param>
    void OnExceptions(DisType distype, ExceptionType exceptiontype)
    {
        NException e = new NException();
        e.m_exceptionType = exceptiontype;
        e.m_exceptionName = distype.ToString();
        for (int i = 0; i < _listExceptions.Count; i++)
        {
            if (_listExceptions[i].m_exceptionType == e.m_exceptionType && _listExceptions[i].m_exceptionName == e.m_exceptionName)
            {
                _listExceptions.Add(e);
                break;
            }
        }
    }
}