using Google.Protobuf;
using Net.ProtolJava;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameManager : BaseManager
{
    static InGameManager m_Instance;
    public new static string NAME = "InGameManager";
    public new static InGameManager Instance
    {
        get
        {
            if (m_Instance == null) m_Instance = GetInstance<InGameManager>(NAME);
            return m_Instance;
        }
    }

    static int frameCheckSize = 300;//检测帧范围
    static int frameKeepSize = 1200;//保留帧数

    public float lookSensitivity = 0.1f;

    GameObject MyselfCharactor;
    GameObject EnemyCharactor;

    Dictionary<string,ActorPlayer> m_Players = new Dictionary<string, ActorPlayer>();
    public ActorPlayer MyselfPlayer;

    public Dictionary<int, ActorPlayerCommand> ClientCommandData = new Dictionary<int, ActorPlayerCommand>(); //保存客户端的命令信息（帧号，命令）
    public Dictionary<int, IMessage> ServerResultData = new Dictionary<int, IMessage>(); //保存服务器端运算的结果信息
    public int ClientFrameReal = -1;//客户端当前帧真实
    public int ClientFrame = -1;//客户端当前表现帧
    public int ServerStartFrame = -1;//服务器开始帧
    public int ServerMaxFrame = -1;//服务器最大帧

    int lastClientFrameReal = -1;
    float lastFrameTime = 0;

    UDPClient udpClient;
    public bool isGameStart = false;//以客户端收到的第一次服务器消息为游戏开始，正式开始刷新
    bool cursorLocked = true;

    int frameStuckCount = 0;
    int frameStuckTimes = 0;

    Dictionary<string,int> framePlayerCount = new Dictionary<string, int>();

    GlobalProxy globalProxy = (GlobalProxy)NGlobal.Facade.RetrieveProxy(GlobalProxy.NAME);
    static int OverServerMaxFrame = Const.OverServerMaxFrame;
    static int OverServerMinFrame = Const.OverServerMinFrame;
    static float OverServerFrameK = 1;

    /////////////Mock
    Dictionary<string, Vector3> mockPlayerPosition = new Dictionary<string, Vector3>();
    
    private void Start()
    {
        MyselfCharactor = Resources.Load<GameObject>("Soldier76");
        EnemyCharactor = Resources.Load<GameObject>("Soldier76");
        ObjectPoolManager.Instance.Register(MyselfCharactor.name, MyselfCharactor, 0, 2);
        ObjectPoolManager.Instance.Register(EnemyCharactor.name, EnemyCharactor, 0, 30);
    }

    public void InitGame()
    {
        isGameStart = false;
        lastClientFrameReal = -1;
        ClientFrameReal = -1;
        ClientFrame = -1;
        frameStuckCount = 0;
        ServerStartFrame = -1;
        ServerMaxFrame = -1;
        if (udpClient != null)
        {
            udpClient.Stop();
        }
        udpClient = new UDPClient();
        udpClient.Start(globalProxy.GetUdpIp(), globalProxy.GetUdpPort(), globalProxy.GetRoomId(), globalProxy.GetUUid(), OnReceivePackage);
    }

    void ChangeLayer(Transform trans, string targetLayer)
    {
        if (LayerMask.NameToLayer(targetLayer) == -1)return;
        trans.gameObject.layer = LayerMask.NameToLayer(targetLayer);
        foreach (Transform child in trans)
        {
            ChangeLayer(child, targetLayer);
        }
    }

    public void RemovePlayer(string uuid)
    {
        if (m_Players.ContainsKey(uuid))
        {
            ActorPlayer actorPlayer = m_Players[uuid];
            string actorName = (actorPlayer.playerId == MyselfPlayer.playerId) ? MyselfCharactor.name : EnemyCharactor.name;
            if (actorPlayer.playerId == MyselfPlayer.playerId) return;
            m_Players.Remove(uuid);
            actorPlayer.Release();
            ObjectPoolManager.Instance.Release(actorName, actorPlayer.gameObject);
        }
    }

    public ActorPlayer AddPlayer(string uuid,bool isMyself)
    {
        string actorName = isMyself ? MyselfCharactor.name: EnemyCharactor.name;
        GameObject playerObj = ObjectPoolManager.Instance.Instantiate(actorName);
        playerObj.SetActive(true);
        ChangeLayer(playerObj.transform, isMyself ? "PlayerMyself" : "PlayerOthers");
        playerObj.layer = LayerMask.NameToLayer("PlayerCharacter");
        playerObj.transform.position = new Vector3(0,8,-8);
        playerObj.name = uuid;
        ActorPlayer actorPlayer = playerObj.GetComponent<ActorPlayer>();
        actorPlayer.Init();
        actorPlayer.isPlayer = true;
        actorPlayer.playerId = uuid;
        if (isMyself)
        {
            actorPlayer.OpenCamera();
            MyselfPlayer = actorPlayer;
        }
        else
        {
            actorPlayer.CloseCamera();
        }
        if (!m_Players.ContainsKey(uuid))
        {
            m_Players.Add(uuid, actorPlayer);
        }
        else
        {
            Debugger.LogError("Has Same Player UUid:"+uuid);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
            return null;
        }
        return actorPlayer;
    }

    public void StartGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        isGameStart = true;
        ClientFrame = 0;
        ClientFrameReal = 0;
        InputManager.Instance.SetMainCamera(MyselfPlayer.m_Camera);
    }



    void OnMyselfFixedUpdate()
    {
        if (!isGameStart || ClientFrameReal < 0 || MyselfPlayer == null || !MyselfPlayer.HasReadyPlay()) return;
        //我方
        int nextFrame = ClientFrameReal + 1;
        if (!ClientCommandData.ContainsKey(nextFrame))
        {
            ActorPlayerCommand command = BuildCommand(nextFrame, m_Players);
            ClientCommandData.Add(nextFrame, command);
            SendFrameCommand(ClientCommandData[nextFrame]);
        }
        ExecuteCommand(ClientCommandData[nextFrame]);
        ClientFrameReal++;
    }

    private void FixedUpdate()
    {
        if (!isGameStart || ClientFrameReal < 0) return;

        //本地操作不等服务器
        {
            frameStuckCount++;
            if (frameStuckCount > Const.FrameStuckLimit) //若阻塞Const.FrameStuckLimit帧，主动跳过
            {
                frameStuckTimes++;
                //Debugger.LogError("ClientFrame Stuck " + frameStuckCount +" Frames At " + ClientFrame);
                ClientFrame++;
            }
            int[] closestRange = GetClosestValidServerFrame(ClientFrame+1);
            int leftFrame = closestRange[0];
            int rightFrame = closestRange[1];
            //Debugger.LogWarning(leftFrame+":"+rightFrame + "|=>|" + ClientFrame + "|" + ClientFrameReal);
            if (rightFrame > ClientFrame && leftFrame > 0)//有多余的服务器帧可表现
            {
                if (ClientFrame < leftFrame)
                {
                    ClientFrame = leftFrame;
                }
                for (int i = ClientFrame + 1; i <= rightFrame && i <= ClientFrameReal; i++)
                {
                    ExecuteCommand(ClientCommandData[i]);
                    ClientFrame = i;
                    break;
                }
                frameStuckCount = 0;
            }
            

            //严格模式
            if ((rightFrame> 0 && rightFrame > ClientFrameReal + 1))
            {
                Debugger.LogError("ClientFrameReal Slower Than ServerFrame!!!!"+(rightFrame - ClientFrameReal));
                ClientFrameReal = rightFrame;
            }

            //容错模式
            /*if ((ServerMaxFrame > 0 && ServerMaxFrame > ClientFrameReal + 1))
            {
                Debugger.LogError("ClientFrameReal Slower Than ServerMaxFrame!!!!" + (ServerMaxFrame - ClientFrameReal));
                ClientFrameReal = ServerMaxFrame;
                ClientFrame = ServerMaxFrame;
            }*/
        }
        OnMyselfFixedUpdate();
        int overFrameCount = ClientFrameReal - ServerMaxFrame;
        OverServerFrameK = udpClient.retryMS / (Const.FrameTime * 1000);
        if (OverServerFrameK < 1) OverServerFrameK = 1;
        OverServerMaxFrame = Mathf.FloorToInt((float)Const.OverServerMaxFrame * OverServerFrameK);
        OverServerMinFrame = Mathf.FloorToInt((float)Const.OverServerMinFrame * OverServerFrameK);
        if (overFrameCount > OverServerMaxFrame && ServerMaxFrame > 0 && frameStuckCount < Const.OverServerFrameFixed)
        {
            float k = (overFrameCount - OverServerMaxFrame) / Const.OverServerFrameFixed;
            if (k < 0) k = 0;
            if (k <= 1)
            {
                Time.fixedDeltaTime = Const.FrameTime * (1 + k);
            }
        }
        else if(overFrameCount < OverServerMinFrame && ServerMaxFrame > 0)
        {
            float k = (OverServerMinFrame - overFrameCount) / Const.OverServerFrameFixed;
            if (k > 0.25f) k = 0.25f;
            if (k < 0) k = 0;
            Time.fixedDeltaTime = Const.FrameTime * (1-k);
        }
        else
        {
            Time.fixedDeltaTime = Const.FrameTime;
        }
    }

    public void ForceWipeLocalFrame(int from,int to)
    {

    }

    public void OnBeforeSendFrameCommand(ActorPlayerCommand command)
    {

    }

    public void OnAfterSendFrameCommand(ActorPlayerCommand command)
    {

    }

    public ActorPlayerCommand BuildCommand(int sequence,Dictionary<string,ActorPlayer> actorPlayers)
    {
        ActorPlayerCommand command = new ActorPlayerCommand();
        command.sequence = sequence;
        //foreach(var item in actorPlayers)
        {
            ActorPlayer actorPlayer = MyselfPlayer;
            ActorPlayerCommandInput input = actorPlayer.BuildCommmandInput(actorPlayer == MyselfPlayer && !MyselfPlayer.isMock);
            ActorPlayerCommandResult result = actorPlayer.BuildCommmandResult(input);
            command.inputs.Add(actorPlayer.playerId, input);
            command.results.Add(actorPlayer.playerId, result);
        }
        command.flags = (int)ActorCommandFlags.None;
        return command;
    }

    public ActorPlayerCommand BuildCommandFromServer(int sequence, PlayerPush playerPush)
    {
        ActorPlayerCommand command = new ActorPlayerCommand();
        command.sequence = sequence;
        Dictionary<string, bool> playerIdMap = new Dictionary<string, bool>();
        for(int i = 0; i < playerPush.Player.Count; i++)
        {
            Player player = playerPush.Player[i];
            ActorPlayer actorPlayer = GetPlayerByUUid(player.Pid);
            playerIdMap.Add(player.Pid, true);
            if (actorPlayer == null)
            {
                Debugger.LogWarning("Add Player uuid =>" + player.Pid+"|"+ sequence+"|"+ClientFrame+"|"+ClientFrameReal+"|"+ServerMaxFrame);
                actorPlayer = AddPlayer(player.Pid,false);
            }
            if (actorPlayer.HasReadyPlay() && actorPlayer!=MyselfPlayer)
            {
                ActorPlayerCommandInput input = new ActorPlayerCommandInput();
                input.horizontalMove = player.Move.HMove;
                input.verticalMove = player.Move.VMove;
                input.horizontalLookMove = player.Move.HLookMove;
                input.verticalLookMove = player.Move.VLookMove;
                input.position = new Vector3(player.Move.Position.X, player.Move.Position.Y, player.Move.Position.Z);
                input.moveVelocity = new Vector3(player.Move.Velocity.X, player.Move.Velocity.Y, player.Move.Velocity.Z);
                input.rotationX = player.Move.HRotation;
                input.rotationY = player.Move.VRotation;
                input.jump = player.Move.Jump;
                input.hasHit = player.Move.HasHit;
                input.aimDir = new Vector3(player.Move.AimDir.X, player.Move.AimDir.Y, player.Move.AimDir.Z);
                input.hit = new Vector3(player.Move.Hit.X, player.Move.Hit.Y, player.Move.Hit.Z);

                input.shootStart = player.Shoot.ShootStart;
                input.shooting = player.Shoot.Shooting;
                input.shootEnd = player.Shoot.ShootEnd;
                ActorPlayerCommandResult result = actorPlayer.BuildCommmandResult(input);

                command.inputs.Add(actorPlayer.playerId, input);
                command.results.Add(actorPlayer.playerId, result);
            }
        }
        if (Const.OpenDebugInfo)
        {
            string[] playerIdSorted = new string[playerIdMap.Count];
            int i = 0;
            foreach (var item in playerIdMap)
            {
                playerIdSorted[i] = item.Key;
                i++;
            }
            Array.Sort<string>(playerIdSorted);
            string key = String.Join(",",playerIdSorted);
            if (!framePlayerCount.ContainsKey(key))
            {
                framePlayerCount.Add(key, 0);
            }
            framePlayerCount[key]++;
        }
        foreach(var item in m_Players)
        {
            if (playerIdMap.ContainsKey(item.Key))
            {
                playerIdMap.Remove(item.Key);
            }
        }
        foreach (var item in playerIdMap)
        {
            RemovePlayer(item.Key);
        }
        command.flags = (int)ActorCommandFlags.None;
        return command;
    }

    public void SendFrameCommand(ActorPlayerCommand command)
    {
        OnBeforeSendFrameCommand(command);
        DoSendFrameCommand(command);
        OnAfterSendFrameCommand(command);
    }

    void DoSendFrameCommand(ActorPlayerCommand command)
    {
        if ((command.flags & (int)ActorCommandFlags.VERIFIED) != 0) return;//已经来自服务器校验的Command，本地比服务器慢了
        ActionRequest actionRequest = new ActionRequest();
        MoveRequest moveRequest = new MoveRequest();
        ShootRequest shootRequest = new ShootRequest();

        ActorPlayerCommandInput input = command.inputs[MyselfPlayer.playerId];
        ActorPlayerCommandResult result = command.results[MyselfPlayer.playerId];

        moveRequest.Position = input.position.ToPV3();
        moveRequest.Velocity = input.moveVelocity.ToPV3();
        moveRequest.HRotation = input.rotationX;
        moveRequest.VRotation = input.rotationY;
        moveRequest.HMove = input.horizontalMove;
        moveRequest.VMove = input.verticalMove;
        moveRequest.HLookMove = input.horizontalLookMove;
        moveRequest.VLookMove = input.verticalLookMove;
        moveRequest.Jump = input.jump;

        moveRequest.AimDir = input.aimDir.ToPV3();
        moveRequest.HasHit = input.hasHit;
        moveRequest.Hit = input.hit.ToPV3();

        shootRequest.ShootStart = input.shootStart;
        shootRequest.Shooting = input.shooting;
        shootRequest.ShootEnd = input.shootEnd;

        actionRequest.MoveRequest = moveRequest;
        actionRequest.ShootRequest = shootRequest;

        byte[] bytes = new byte[actionRequest.CalculateSize()];
        var stream = new CodedOutputStream(bytes);
        actionRequest.WriteTo(stream);
        stream.Flush();
        udpClient.ActionRequest(command.sequence, bytes);
    }
    
    void CheckNeedRemoveItem(int frame)
    {
        if (ClientCommandData.ContainsKey(frame - frameKeepSize))
        {
            ClientCommandData.Remove(frame - frameKeepSize);
        }
        if (ServerResultData.ContainsKey(frame - frameKeepSize))
        {
            ServerResultData.Remove(frame - frameKeepSize);
        }
    }

    public void ExecuteCommand(ActorPlayerCommand command)
    {
        CheckNeedRemoveItem(command.sequence);
        bool otherPlayerHasExecuted = (command.flags & (int)ActorCommandFlags.HAS_EXECUTED_OTHERS) != 0;
        bool selfHasExecuted = (command.flags & (int)ActorCommandFlags.HAS_EXECUTED_SELF) != 0;
        if (selfHasExecuted && otherPlayerHasExecuted)return;
        bool curOtherPlayerHasExecuted = false;
        foreach (var item in command.inputs)
        {
            bool needExecute = true ;
            
            if (item.Key != MyselfPlayer.playerId)
            {
                if (otherPlayerHasExecuted) needExecute = false;
                curOtherPlayerHasExecuted = true;
            }
            else
            {
                if (selfHasExecuted) needExecute = false;
                selfHasExecuted = true;
            }
            if (needExecute)
            {
                ActorPlayer actorPlayer = GetPlayerByUUid(item.Key);
                ActorPlayerCommandResult result = command.results[item.Key];
                actorPlayer.UpdatePlayerByCommandResult(result);
            }
        }
        if (curOtherPlayerHasExecuted)
        {
            //Debugger.Log("ExecuteFrame=>" + command.sequence+"|"+ClientFrameReal);
        }
        if(selfHasExecuted) command.flags |= (int)ActorCommandFlags.HAS_EXECUTED_SELF;
        if(curOtherPlayerHasExecuted) command.flags |= (int)ActorCommandFlags.HAS_EXECUTED_OTHERS;
        
    }

    void OnReceivePackage(FpsUdpPacketHead fpsUdpPacketHead, byte[] dataBytes)
    {
        int frame = fpsUdpPacketHead.MasterPacketNum;
        ResultPush resultPush = ResultPush.Parser.ParseFrom(dataBytes);

        //MockPlayerPush(resultPush.PlayerPush);

        if (!ServerResultData.ContainsKey(frame))
        {
            if (ServerStartFrame < 0)
            {
                ServerStartFrame = frame;
                ClientFrame = frame;
                ClientFrameReal = frame;
                frameStuckCount = 0;
            }
            if (frame > ServerMaxFrame) ServerMaxFrame = frame;
            ServerResultData.Add(frame, resultPush);
        }
        if (!ClientCommandData.ContainsKey(frame))
        {
            ClientCommandData.Add(frame, BuildCommandFromServer(frame, resultPush.PlayerPush));
        }
        else
        {
            ActorPlayerCommand command = BuildCommandFromServer(frame, resultPush.PlayerPush);
            /*if (!CheckCommand(ClientCommandData[frame], command))
            {
                Debugger.LogError("包数据错误：帧号为" + fpsUdpPacketHead.MasterPacketNum);
            }
            else*/
            {
                foreach(var item in command.inputs)
                {
                    if (!ClientCommandData[frame].inputs.ContainsKey(item.Key))
                    {
                        ClientCommandData[frame].inputs.Add(item.Key, item.Value);
                        ClientCommandData[frame].results.Add(item.Key, command.results[item.Key]);
                    }
                }
                //ClientCommandData[frame].inputs = command.inputs;
                //ClientCommandData[frame].results = command.results;
            }
        }
        ClientCommandData[frame].flags |= (int)ActorCommandFlags.VERIFIED;
        //Debugger.LogWarning("收到数据包：帧号为" + fpsUdpPacketHead.MasterPacketNum  + "包大小为" + dataBytes.Length);
    }

    bool CheckCommand(ActorPlayerCommand c1, ActorPlayerCommand c2)
    {
        bool result = true;
        foreach(var item in c2.inputs)
        {
            if (c1.inputs.ContainsKey(item.Key))
            {
                if ((c2.results[item.Key].rotationX - c1.results[item.Key].rotationX) > 0) result = false;
                if ((c2.results[item.Key].rotationY - c1.results[item.Key].rotationY) > 0) result = false;
                if ((c2.results[item.Key].position - c1.results[item.Key].position).sqrMagnitude > 0) result = false;
                if ((c2.results[item.Key].movement - c1.results[item.Key].movement).sqrMagnitude > 0) result = false;
                if ((c2.results[item.Key].moveVelocity - c1.results[item.Key].moveVelocity).sqrMagnitude > 0) result = false;
                if(result == false)
                {
                    string errorLog = "";
                    errorLog += c2.results[item.Key].rotationX + "|" + c1.results[item.Key].rotationX + "\n";
                    errorLog += c2.results[item.Key].rotationY + "|" + c1.results[item.Key].rotationY + "\n";
                    errorLog += c2.results[item.Key].position + "|" + c1.results[item.Key].position + "\n";
                    errorLog += c2.results[item.Key].movement + "|" + c1.results[item.Key].movement + "\n";
                    errorLog += c2.results[item.Key].moveVelocity + "|" + c1.results[item.Key].moveVelocity + "\n";
                    Debugger.LogError(errorLog);
                    return result;
                }
            }
            
        }
        return result;
    }

    int[] GetClosestValidServerFrame(int curClientFrame)
    {
        int curFrameLeft = 0;
        int curFrameRight = 0;
        if (ServerStartFrame > curClientFrame)
        {
            curFrameRight = (ServerStartFrame > 0) ? ServerStartFrame : 0;
            curFrameLeft = curFrameRight;
            return new int[] { curFrameLeft, curFrameRight };
        }
        curFrameLeft = curClientFrame;
        curFrameRight = curClientFrame;
        bool hasBreakLeft = false;
        bool hasBreakRight = false;
        for (int i=0;i< frameCheckSize; i++)
        {
            if(!hasBreakLeft)
            {
                curFrameLeft--;
                if (!ServerResultData.ContainsKey(curFrameLeft))
                {
                    hasBreakLeft = true;
                    curFrameLeft++;
                }
            }
            if (!hasBreakRight)
            {
                curFrameRight++;
                if (!ServerResultData.ContainsKey(curFrameRight))
                {
                    hasBreakRight = true;
                    curFrameRight--;
                }
            }
            if(hasBreakLeft && hasBreakRight)
            {
                break;
            }
        }
        if (!ServerResultData.ContainsKey(curFrameLeft)) curFrameLeft = 0;
        if (!ServerResultData.ContainsKey(curFrameRight)) curFrameRight = 0;
        return new int[]{ curFrameLeft,curFrameRight};
    }
    
    ActorPlayer GetPlayerByUUid(string uuid)
    {
        if (!m_Players.ContainsKey(uuid)) return null;
        return m_Players[uuid];
    }

    private void OnApplicationQuit()
    {
        if(udpClient!=null)udpClient.Stop();
    }

    private void Update()
    {
        if(udpClient != null)
            udpClient.DelReceivePackage();

        if (Input.GetKeyDown(KeyCode.M))
        {
            //MockAddEnemy();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            ActorPlayerRandomMock aprm = MyselfPlayer.gameObject.GetComponent<ActorPlayerRandomMock>();
            if(aprm == null)
            {
                aprm = MyselfPlayer.gameObject.AddComponent<ActorPlayerRandomMock>();
                MyselfPlayer.isMock = true;
                aprm.m_Players = new ActorPlayer[] { MyselfPlayer };
            }
            else
            {
                if (aprm.enabled)
                {
                    aprm.enabled = false;
                    MyselfPlayer.isMock = false;
                }
                else
                {
                    aprm.enabled = true;
                    MyselfPlayer.isMock = true;
                }
                
            }
            //MockAddEnemy();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (cursorLocked)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            cursorLocked = !cursorLocked;
        }
    }

    void MockAddEnemy()
    {
        mockPlayerPosition.Add("mock_"+ mockPlayerPosition.Count, new Vector3(0, 0, -2 * mockPlayerPosition.Count));
    }

    void MockPlayerPush(PlayerPush playerPush)
    {
        Player master = null;
        for(int i = 0; i < playerPush.Player.Count; i++)
        {
            if(MyselfPlayer.playerId == playerPush.Player[i].Pid)
            {
                master = playerPush.Player[i];
                break;
            }
        }
        if (master == null) return;
        foreach(var item in mockPlayerPosition)
        {
            Player player = new Player();
            player.Pid = item.Key;
            player.Move = new MoveRequest();
            player.Shoot = new ShootRequest();

            player.Move.MergeFrom(master.Move);
            player.Move.Position.X += item.Value.x;
            player.Move.Position.Y += item.Value.y;
            player.Move.Position.Z += item.Value.z;
            player.Shoot.MergeFrom(master.Shoot);
            playerPush.Player.Add(player);
        }
        
    }

    public int GetUDPPing()
    {
        if (udpClient != null)
        {
            return udpClient.retryMS;
        }
        return -1;
    }

    public int GetClientCommandSize()
    {
        return ClientCommandData.Count;
    }
    public int GetServerDataSize()
    {
        return ServerResultData.Count;
    }

    public int GetFrameStuckTimes()
    {
        return frameStuckTimes;
    }

    public float GetOverServerFrameK()
    {
        return OverServerFrameK;
    }

    public string GetFramePlayerCount()
    {
        //temp
        string info = "\n";
        foreach(var item in framePlayerCount)
        {
            info += "[" + item.Key + "]:" + item.Value + "\n";
        }
        return info;
    }

    public bool IsMe(ActorPlayer actor)
    {
        return MyselfPlayer == actor;
    }
}
