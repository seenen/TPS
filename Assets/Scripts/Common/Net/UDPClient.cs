using UnityEngine;
using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Google.Protobuf;
using System.Linq;
using Net.ProtolJava;

public class UDPPacket
{
    public FpsUdpPacketHead headInfo { get; private set; }
    public byte[] headBuffer { get; private set; }
    public byte[] allBuffer { get; private set; }
    public DateTime tryTime { get; private set; }   //重发尝试时间
    public int retryCount { get; private set; } //重试次数
    public int retryMS { get; private set; } //重试次数
    public bool bReliable { get; private set; } //是否需要重发Udp
    public DateTime rtt { get; private set; } //网络时延
    public UDPPacket(FpsUdpPacketHead head , byte[] b , bool reliable, int retryMS)
    {
        rtt = DateTime.Now;
        headInfo = head;
        headBuffer = b;
        tryTime = DateTime.Now.AddMilliseconds(-retryMS* UDPClient.retryMSRatio);
        bReliable = reliable;
        this.retryMS = retryMS;
        allBuffer = GetAllBytes();
    }

    public byte[] GetAllBytes()
    {
        short headSize = (short)headInfo.CalculateSize();
        //前2个字节代表FpsUdpPacketHead 包头长度
        byte[] allbuffer = new byte[headBuffer.Length + headSize + 2];
        //FpsUdpPacketHead 包头长度
        byte[] headSizeBytes = BitConverter.GetBytes(headSize);
        //FpsUdpPacketHead 字节数组
        byte[] headBytes = new byte[headSize];
        var stream = new CodedOutputStream(headBytes);
        headInfo.WriteTo(stream);

        Array.Reverse(headSizeBytes);//由于c#和Java字节序不同，需要反一下，其他的proto做过处理所以不用反

        Array.ConstrainedCopy(headSizeBytes, 0, allbuffer, 0, headSizeBytes.Length);//FpsUdpPacketHead大小2字节
        Array.ConstrainedCopy(headBytes, 0, allbuffer, 2, headSize);//FpsUdpPacketHead
        Array.ConstrainedCopy(headBuffer, 0, allbuffer, 2 + headSize, headBuffer.Length);
        return allbuffer;
    }
    public bool bOutOfTime { get; private set; }    //Udp包是否超过重发次数
    public bool NeedRetry   //是否可以重连
    {
        get
        {
            if ((DateTime.Now - tryTime).TotalMilliseconds < retryMS * UDPClient.retryMSRatio)
            {
                return false;
            }
            tryTime = DateTime.Now;
            retryCount++;
            if (retryCount > UDPClient.maxRetryCount)
            {
                bOutOfTime = true;
                return false;
            }
            return true;
        }
    }
}

public delegate void UDPClientReceivePackage(FpsUdpPacketHead fpsUdpPacketHead, byte[] data);

public class UDPClient
{
    public static bool debug = false;
    public const float retryMSRatio = 1.6f;//rtt系数 retryMS*retryMSRatio为重发包间隔
    public const int maxRetryCount = 10;//最大重试次数


    public int retryMS = 200;//暂定200ms秒没收到确认包重发 rtt

    public string roomId = "";
    public string uuid = "";
    public UDPClientReceivePackage receiveListener = null;

    private int sendMessageTime = 0;
    private string UDPClientIP;
    private int UDPClientPort;
    private int maxBufferSize = 500;// 一个udp包的长度控制为548，-2包头长度，预留46个字节给包头
    private object sendLock = new object();

    private Socket socket;
    private EndPoint serverEnd;
    private IPEndPoint ipEnd;
    private Thread sendThread;//发包的线程
    private Thread connectThread;//收包的线程
    private byte[] recvData;//收到的数据
    private Dictionary<int, UDPPacket> packetsToSendPool = new Dictionary<int, UDPPacket>();//存储udp的发包 key为主帧号*1000+副帧号*10+是否确认包
    private Dictionary<int, Dictionary<int, byte[]>> dPendingPackets = new Dictionary<int, Dictionary<int, byte[]>>();//存储待处理的包（拆过的包），等全收到一并处理
    private List<byte[]> packetsToRecievePool = new List<byte[]>();
    public void Start(string ip, int port, string roomId, string uuid, UDPClientReceivePackage receiveListener)
    {
        UDPClientIP = ip.Trim();
        UDPClientPort = port;
        this.roomId = roomId;
        this.uuid = uuid;
        this.receiveListener = receiveListener;
        InitUdpSocket();
    }
    /// <summary>
    /// 初始化网络
    /// </summary>
    void InitUdpSocket()
    {
        ipEnd = new IPEndPoint(IPAddress.Parse(UDPClientIP), UDPClientPort);
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 4000);

        serverEnd = sender;
        //开启一个线程连接
        connectThread = new Thread(new ThreadStart(SocketReceive));
        connectThread.Start();
        sendMessageTime = (int)(Const.FrameTime * 1000);
        Debugger.Log(sendMessageTime);
        sendThread = new Thread(new ThreadStart(SendUdpPackage));
        sendThread.Start();
    }

    /// <summary>
    /// 每帧发送拆包后的udp包
    /// </summary>
    public void SendUdpPackage()
    {
        while (true)
        {
            lock (sendLock)
            {
                List<int> remove = new List<int>();
                foreach (var pv in packetsToSendPool)
                {
                    var p = pv.Value;
                    if (p.bOutOfTime)
                    {
                        remove.Add(pv.Key);
                        continue;
                    }
                    if (p.NeedRetry)
                    {
                        //short headSize = (short)p.headInfo.CalculateSize();
                        ////前2个字节代表FpsUdpPacketHead 包头长度
                        //byte[] buffer = new byte[p.headBuffer.Length + headSize + 2];
                        ////FpsUdpPacketHead 包头长度
                        //byte[] headSizeBytes = BitConverter.GetBytes(headSize);
                        ////FpsUdpPacketHead 字节数组
                        //byte[] headBytes = new byte[headSize];
                        //var stream = new CodedOutputStream(headBytes);
                        //p.headInfo.WriteTo(stream);

                        //Array.Reverse(headSizeBytes);//由于c#和Java字节序不同，需要反一下，其他的proto做过处理所以不用反

                        //Array.ConstrainedCopy(headSizeBytes, 0, buffer, 0, headSizeBytes.Length);//FpsUdpPacketHead大小2字节
                        //Array.ConstrainedCopy(headBytes, 0, buffer, 2, headSize);//FpsUdpPacketHead
                        //Array.ConstrainedCopy(p.headBuffer, 0, buffer, 2 + headSize, p.headBuffer.Length);

                        socket.SendTo(p.allBuffer, p.allBuffer.Length, SocketFlags.None, ipEnd);
                        if (UDPClient.debug)
                        {
                            if (p.headInfo.EnsurePacket)
                            {
                                string str = "";
                                for (int i = 0; i < p.allBuffer.Length; i++)
                                {
                                    str += p.allBuffer[i] + " ";
                                }
                                Debugger.Log("发送确认包：帧号为" + p.headInfo.MasterPacketNum + "副帧号为" + p.headInfo.SlavePacketNum + "\n" + str);
                            }
                            else
                            {
                                string str = "";
                                for (int i = 0; i < p.allBuffer.Length; i++)
                                {
                                    str += p.allBuffer[i] + " ";
                                }
                                Debugger.Log("发送数据包：帧号为" + p.headInfo.MasterPacketNum + "副帧号为" + p.headInfo.SlavePacketNum + "包大小为" + p.headBuffer.Length + "\n" + str);
                            }
                        }

                        if (!p.bReliable)//不需要重发Udp（确认包），删除
                        {
                            remove.Add(pv.Key);
                        }
                    }
                }
                foreach (var r in remove)
                {
                    packetsToSendPool.Remove(r);
                }
            }
            Thread.Sleep(sendMessageTime);
        }
    }
    /// <summary>
    /// 拆包处理
    /// </summary>
    /// <param name="packetNum">id</param>
    /// <param name="buffer">原始数据</param>
    private void BufferProcesser(int packetNum, byte[] buffer)
    {
        if (buffer.Length <= maxBufferSize)
        {
            int id = packetNum * 1000;
            if (!packetsToSendPool.ContainsKey(id))
            {
                FpsUdpPacketHead fpsUdpPacketHead = new FpsUdpPacketHead();
                fpsUdpPacketHead.RoomId = roomId;
                fpsUdpPacketHead.PlayerId = uuid;
                fpsUdpPacketHead.EnsurePacket = false;
                fpsUdpPacketHead.MasterPacketNum = packetNum;
                fpsUdpPacketHead.Rtt = retryMS;
                UDPPacket packet = new UDPPacket(fpsUdpPacketHead, buffer, true, retryMS);
                packetsToSendPool.Add(packetNum * 1000, packet);
            }
        }
        else
        {
            int totalBuffSec = Mathf.CeilToInt((float)buffer.Length / maxBufferSize);
            int secondFrame = 1;
            int offset = 0;
            for (int i = 0; i < totalBuffSec; i++)
            {
                int id = packetNum * 1000 + secondFrame * 10;
                int size = buffer.Length - offset > maxBufferSize ? maxBufferSize : buffer.Length - offset;
                if (!packetsToSendPool.ContainsKey(id))
                {
                    FpsUdpPacketHead fpsUdpPacketHead = new FpsUdpPacketHead();
                    fpsUdpPacketHead.RoomId = roomId;
                    fpsUdpPacketHead.PlayerId = uuid;
                    fpsUdpPacketHead.EnsurePacket = false;
                    fpsUdpPacketHead.MasterPacketNum = packetNum;
                    fpsUdpPacketHead.SlavePacketNum = secondFrame;
                    fpsUdpPacketHead.TotalSlavePacketNum = totalBuffSec;
                    byte[] bs = new byte[size];
                    bs = buffer.Skip(offset).Take(size).ToArray();
                    UDPPacket packet = new UDPPacket(fpsUdpPacketHead, bs, true, retryMS);
                    packetsToSendPool.Add(packetNum * 1000 + secondFrame * 10, packet);
                }
                offset += size;
                secondFrame++;
            }
        }
    }
    /// <summary>
    /// 组合包处理
    /// </summary>
    /// <param name="dictionary">拆包集合</param>
    private void ProcessingPacket(FpsUdpPacketHead fpsUdpPacketHead, Dictionary<int, byte[]> dictionary)
    {
        int mainFrame = fpsUdpPacketHead.MasterPacketNum;
        byte[] buffer = new byte[maxBufferSize * dictionary.Count];
        int total = 0;
        for (int i = 0; i < dictionary.Count; i++)
        {
            Array.Copy(dictionary[i], 0, buffer, total, dictionary[i].Length);
            total += dictionary[i].Length;
        }
        byte[] result = new byte[total];
        Array.Copy(buffer, 0, result, 0, result.Length);
        receiveListener?.Invoke(fpsUdpPacketHead, result);
    }

    //发送数据包
    public void ActionRequest(int packetNum, byte[] bytes)
    {
        lock (sendLock)
        {
            BufferProcesser(packetNum, bytes);
        }
    }

    //发送确认包
    public void EnsurePacketRequest(FpsUdpPacketHead reciveFpsUdpPacketHead)
    {
        int id = reciveFpsUdpPacketHead.MasterPacketNum * 1000 + 1;
        if (!packetsToSendPool.ContainsKey(id))
        {
            FpsUdpPacketHead fpsUdpPacketHead = new FpsUdpPacketHead();
            fpsUdpPacketHead.RoomId = reciveFpsUdpPacketHead.RoomId;
            fpsUdpPacketHead.PlayerId = reciveFpsUdpPacketHead.PlayerId;
            fpsUdpPacketHead.EnsurePacket = true;
            fpsUdpPacketHead.MasterPacketNum = reciveFpsUdpPacketHead.MasterPacketNum;
            fpsUdpPacketHead.SlavePacketNum = reciveFpsUdpPacketHead.SlavePacketNum;
            fpsUdpPacketHead.TotalSlavePacketNum = reciveFpsUdpPacketHead.TotalSlavePacketNum;
            UDPPacket packet = new UDPPacket(fpsUdpPacketHead, new byte[0], false, retryMS);
            packetsToSendPool.Add(id, packet);
        }
    }

    //接收服务器信息
    void SocketReceive()
    {
        int recvLen = 0;
        while (true)
        {
            if (!socket.Connected)
            {
                Thread.Sleep(sendMessageTime);
                continue;
            }
            recvData = new byte[548];
            try
            {
                recvLen = socket.ReceiveFrom(recvData, ref serverEnd);
                lock (sendLock)
                {
                    if (recvLen > 0)
                    {
                        byte[] allBytes = new byte[recvLen];
                        allBytes = recvData.Take(recvLen).ToArray();
                        packetsToRecievePool.Add(allBytes);
                    }
                }
            }
            catch (Exception e)
            {
                if (UDPClient.debug) Debugger.Log("UDPException : " + e);
            }
        }
    }

    //连接关闭
    public void Stop()
    {
        //关闭线程
        if (connectThread != null)
        {
            connectThread.Interrupt();
            connectThread.Abort();
        }
        //最后关闭socket
        if (socket != null)
            socket.Close();
    }

    public void DelReceivePackage()
    {
        lock (sendLock)
        {
            for (int i = 0; i < packetsToRecievePool.Count; i++)
            {
                byte[] recvData = packetsToRecievePool[i];
                int recvLen = recvData.Length;

                //由于c#和Java字节序不同，需要反一下，其他的proto做过处理所以不用反
                byte[] headSizeBytes = new byte[2];
                headSizeBytes[0] = recvData[1];
                headSizeBytes[1] = recvData[0];
                short headInfoSize = BitConverter.ToInt16(headSizeBytes, 0);
                byte[] FpsUdpPacketHeadBytes = recvData.Skip(2).Take(headInfoSize).ToArray();
                FpsUdpPacketHead fpsUdpPacketHead = FpsUdpPacketHead.Parser.ParseFrom(FpsUdpPacketHeadBytes);

                if (fpsUdpPacketHead.EnsurePacket)//是确认包
                {
                    int id = fpsUdpPacketHead.MasterPacketNum * 1000 + fpsUdpPacketHead.SlavePacketNum * 10;
                    if (UDPClient.debug) Debugger.Log("收到确认包：帧号为" + fpsUdpPacketHead.MasterPacketNum + "副帧号为" + fpsUdpPacketHead.SlavePacketNum);
                    if (packetsToSendPool.ContainsKey(id))
                    {
                        retryMS = (int)(DateTime.Now - packetsToSendPool[id].rtt).TotalMilliseconds;
                        packetsToSendPool.Remove(id);
                    }
                }
                else
                {
                    if (fpsUdpPacketHead.TotalSlavePacketNum == 0)//无拆包处理
                    {
                        EnsurePacketRequest(fpsUdpPacketHead);//发送确认包
                        byte[] dataBytes = recvData.Skip(2 + headInfoSize).Take(recvLen - 2 - headInfoSize).ToArray();
                        if (UDPClient.debug) Debugger.Log("收到数据包：帧号为" + fpsUdpPacketHead.MasterPacketNum + "副帧号为" + fpsUdpPacketHead.SlavePacketNum + "包大小为" + dataBytes.Length);
                        receiveListener?.Invoke(fpsUdpPacketHead, dataBytes);
                    }
                    else //拆包处理
                    {
                        EnsurePacketRequest(fpsUdpPacketHead);//发送确认包
                        int mainFrame = fpsUdpPacketHead.MasterPacketNum;
                        if (!dPendingPackets.ContainsKey(mainFrame))
                        {
                            dPendingPackets.Add(mainFrame, new Dictionary<int, byte[]>());
                        }
                        int index = fpsUdpPacketHead.SlavePacketNum - 1;

                        byte[] bytes = new byte[recvLen - 2 - headInfoSize];
                        bytes = recvData.Skip(2 + headInfoSize).Take(bytes.Length).ToArray();
                        Dictionary<int, byte[]> dPendingPacketsCur = dPendingPackets[mainFrame];
                        if (dPendingPacketsCur.ContainsKey(index))
                        {
                            dPendingPacketsCur[index] = bytes;
                        }
                        else
                        {
                            dPendingPacketsCur.Add(index, bytes);
                        }
                        if (dPendingPacketsCur.Count == fpsUdpPacketHead.TotalSlavePacketNum)
                        {
                            ProcessingPacket(fpsUdpPacketHead, dPendingPacketsCur);
                        }
                        if (UDPClient.debug) Debugger.Log("收到数据包：帧号为" + fpsUdpPacketHead.MasterPacketNum + "副帧号为" + fpsUdpPacketHead.SlavePacketNum + "包大小为" + bytes.Length);
                    }
                }
            }
            packetsToRecievePool.Clear();
        }
    }
}