using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Net.ProtolJava;

public class TestLoginRoom : ViewComponent
{
    public GameObject m_MatchPanel;
    public GameObject m_InGamePanel;


    public InputField m_IpInput;
    public InputField m_PortInput;
    public InputField m_RoomIdInput;
    public InputField m_UuidInput;

    //TempUsage
    GlobalProxy globalProxy;
    private void Awake()
    {
        globalProxy = (GlobalProxy)NGlobal.Facade.RetrieveProxy(GlobalProxy.NAME);
        m_MatchPanel.SetActive(true);
    }
    
    void Start()
    {
        m_IpInput.text = globalProxy.GetIp();
        m_PortInput.text = globalProxy.GetPort().ToString();
        m_RoomIdInput.text = globalProxy.GetRoomId();
        m_UuidInput.text = globalProxy.GetUUid();
        RegisterPush();
    }

    public void RegisterPush()
    {
        NManagerNet.Instance.RegisterPush("MatchSuccessPush", EnterGame, ENetType.Match);
        NManagerNet.Instance.RegisterPush("GameReadyPush", BeginGame, ENetType.Game);
    }

    public void OnStartClick()
    {
        //udp暂时写死
        globalProxy.SetUdpIp(m_IpInput.text);
        globalProxy.SetUdpPort(7001);
        globalProxy.SetIp(m_IpInput.text);
        globalProxy.SetPort(int.Parse(m_PortInput.text));
        globalProxy.SetRoomId(m_RoomIdInput.text);
        globalProxy.SetUUid(m_UuidInput.text);
        PlayerPrefs.Save();
        if (m_IpInput.text == "127.0.0.1")
        {
            BeginGame(ENetType.Count, null, null);
            return;
        }
        NManagerNet.Instance.SetIpAndPort(ENetType.Match, globalProxy.GetIp(), globalProxy.GetPort());
        MatchRequest matchRequest = new MatchRequest();
        matchRequest.PlayerId = globalProxy.GetUUid();
        NManagerNet.Instance.SendMessage(ENetType.Match, "Match", delegate (int error, byte[] data)
        {
            Debugger.Log("recive : MatchResponse");
        }, matchRequest);
    }

    public void EnterGame(ENetType eNetType, string name, byte[] results)
    {
        MatchSuccessPush matchSuccessPush = MatchSuccessPush.Parser.ParseFrom(results);
        if (matchSuccessPush.StatusCode == 0)
        {
            globalProxy.SetRoomId(matchSuccessPush.RoomId);
            NManagerNet.Instance.SetIpAndPort(ENetType.Game, matchSuccessPush.GameServerIp, matchSuccessPush.GameServerPort);
            //由于现在没有加载场景，所以直接发ready
            GameReadyRequest gameReadyRequest = new GameReadyRequest();
            gameReadyRequest.RoomId = globalProxy.GetRoomId();
            gameReadyRequest.PlayerId = globalProxy.GetUUid();
            NManagerNet.Instance.SendMessage(ENetType.Game, "GameReady", delegate (int error1, byte[] data1)
            {
            }, gameReadyRequest);
        }
        else
        {
            Debugger.Log("匹配失败");
        }
    }

    public void BeginGame(ENetType eNetType, string name, byte[] results)
    {
        m_MatchPanel.SetActive(false);
        m_InGamePanel.SetActive(true);
        InGameManager.Instance.InitGame();
        InGameManager.Instance.AddPlayer(globalProxy.GetUUid(), true);
        InGameManager.Instance.StartGame();
        
    }
}
