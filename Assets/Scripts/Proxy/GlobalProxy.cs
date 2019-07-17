using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PureMVC.Patterns.Proxy;
using UnityEngine;

public class GlobalProxy : BaseProxy<GlobalPackage>
{
    public new static string NAME = "GlobalProxy";
    public GlobalProxy(string proxyName, GlobalPackage data) : base(proxyName, data)
    {

    }

    public string GetRoomId()
    {
        if (string.IsNullOrEmpty(Data.roomId))
        {
            Data.roomId = PlayerPrefs.GetString("roomId", "100000");
        }
        return Data.roomId;
    }

    public void SetRoomId(string roomId)
    {
        PlayerPrefs.SetString("roomId", roomId);
        Data.roomId = roomId;
    }

    public string GetUUid()
    {
        if (string.IsNullOrEmpty(Data.uuid))
        {
            Data.uuid = PlayerPrefs.GetString("uuid", "p1");
        }
        return Data.uuid;
    }

    public void SetUUid(string uuid)
    {
        PlayerPrefs.SetString("uuid", uuid);
        Data.uuid = uuid;
    }

    public string GetIp()
    {
        if (string.IsNullOrEmpty(Data.ip))
        {
            Data.ip = PlayerPrefs.GetString("ip", "192.168.90.138");
        }
        return Data.ip;
    }

    public void SetIp(string ip)
    {
        PlayerPrefs.SetString("ip", ip);
        Data.ip = ip;
    }

    public int GetPort()
    {
        if (Data.port == 0)
        {
            Data.port = PlayerPrefs.GetInt("port", 6144);
        }
        return Data.port;
    }

    public void SetPort(int port)
    {
        PlayerPrefs.SetInt("port", port);
        Data.port = port;
    }

    public string GetUdpIp()
    {
        if (string.IsNullOrEmpty(Data.udpIp))
        {
            Data.udpIp = PlayerPrefs.GetString("ip", "192.168.90.138");
        }
        return Data.udpIp;
    }

    public void SetUdpIp(string udpIp)
    {
        PlayerPrefs.SetString("udpIp", udpIp);
        Data.udpIp = udpIp;
    }

    public int GetUdpPort()
    {
        if (Data.udpPort == 0)
        {
            Data.udpPort = PlayerPrefs.GetInt("port", 6143);
        }
        return Data.udpPort;
    }

    public void SetUdpPort(int udpPort)
    {
        PlayerPrefs.SetInt("udpPort", udpPort);
        Data.udpPort = udpPort;
    }
}