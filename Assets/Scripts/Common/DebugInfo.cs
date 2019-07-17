
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;


public class DebugInfo : MonoBehaviour
{
    private static string GL_JAVA_CLASS = "com.nkmhook.GLHooked";
    AndroidJavaObject androidAgentGL = null;
    private float updateInterval = 0.5F;
    private double lastInterval;
    private int frames = 0;
    private float fps;
    private string debugInfo = "";
    StringBuilder debug = new StringBuilder();

    // Use this for initialization
    void Start()
    {
        if (!Const.OpenDebugInfo) return;
        lastInterval = Time.realtimeSinceStartup;
        frames = 0;

#if UNITY_ANDROID && !UNITY_EDITOR
        androidAgentGL = new AndroidJavaObject(GL_JAVA_CLASS);
#endif
    }

    // Update is called once per frame
    void Update()
    {
        if (!Const.OpenDebugInfo) return;
        ++frames;
        float timeNow = Time.realtimeSinceStartup;
        if (timeNow > lastInterval + updateInterval)
        {
            fps = (float)(frames / (timeNow - lastInterval));
            frames = 0;
            lastInterval = timeNow;
            debugInfo = GetDebugShowInfo();
        }
    }
#if !FPS_STABLE
    void OnGUI()
    {
        GUI.skin.textField.normal.textColor = new Color(0, 1, 0);
        GUI.skin.textField.fontSize = 20;
        GUI.skin.textField.fontStyle = FontStyle.Normal;
        GUI.skin.textField.padding = new RectOffset(10, 10, 10, 10);
        GUI.skin.label = GUI.skin.textField;
        if (!Const.OpenDebugInfo) return;
        GUILayout.Label(debugInfo);
    }
#endif

    string GetDebugShowInfo()
    {
        var monoUsed = (Profiler.GetMonoUsedSizeLong() >> 10) / 1024f;
        var monoTotal = (Profiler.GetMonoHeapSizeLong() >> 10) / 1024f;
        var unityUsed = (Profiler.GetTotalAllocatedMemoryLong() >> 10) / 1024f;
        var unityTotal = (Profiler.GetTotalReservedMemoryLong() >> 10) / 1024f;
        var gc = GC.CollectionCount(0);
        debug.Clear();
        debug.Append("FPS: ").Append(fps.ToString("f1")).Append("/").Append((1f / Time.fixedDeltaTime).ToString("f1")).Append("\n")
            .Append("GC: ").Append(gc).Append("\n")
            .Append("Drawcalls: ").Append(getDrawcalls()).Append("\n")
            .Append("Triangles: ").Append(getTriangles()).Append("\n")
            .Append("MonoUsed: ").Append(monoUsed.ToString("f1")).Append(" MB").Append("\n")
            .Append("MonoUsed: ").Append(monoUsed.ToString("f1")).Append(" MB").Append("\n")
            .Append("MonoTotal: ").Append(unityUsed.ToString("f1")).Append(" MB").Append("\n")
            .Append("UnityTotal: ").Append(unityTotal.ToString("f1")).Append(" MB").Append("\n")
            ;

        InGameManager inGameManager = (InGameManager)NGlobal.Facade.RetrieveMediator(InGameManager.NAME);
        if (inGameManager != null)
        {
            debug.Append("UDPPing: ").Append(inGameManager.GetUDPPing()).Append("ms\n")
                .Append("FrameStartServer: ").Append(inGameManager.ServerStartFrame).Append("\n")
                .Append("FrameMaxServer: ").Append(inGameManager.ServerMaxFrame).Append("/").Append(inGameManager.GetServerDataSize()).Append("(").Append(inGameManager.ServerMaxFrame - inGameManager.ClientFrame).Append(")\n")
                .Append("FrameLocal: ").Append(inGameManager.ClientFrameReal).Append("(").Append(inGameManager.ClientFrameReal - inGameManager.ClientFrame).Append(")").Append(inGameManager.GetOverServerFrameK()).Append("\n")
                .Append("FramePlayed: ").Append(inGameManager.ClientFrame).Append("/").Append(inGameManager.GetClientCommandSize()).Append("\n")
                .Append("FrameStuck: ").Append(inGameManager.GetFrameStuckTimes()).Append("\n")
                .Append("FramePlayerCount: ").Append(inGameManager.GetFramePlayerCount()).Append("\n");
        }
        /*if (App.LuaManager != null)
        {
            var lua = App.LuaManager.CallFunction<int>("LuaMemory") / 1024f;
            debug += "LuaUsed: " + lua.ToString("f1") + " MB" + "\n";
        }
        debug += "NetworkType: " + Util.GetNetworkType() + "\n";
        debug += "NetworkStrength: " + Util.GetNetworkStrength() + "\n";
        if (App.LuaManager != null && App.NetworkManager != null)
        {
            var socketConfigs = App.NetworkManager.GetAllConfig();
            foreach(var item in socketConfigs)
            {
                debug += "Socket["+item.Key+"]: " + App.LuaManager.CallFunction<string>("GetSocketPingInfo", item.Key) + "\n";
            }
        }*/
        return debug.ToString().Substring(0, debug.Length - 1);
    }

    int getFPSTime()
    {
        if (androidAgentGL != null)
        {
            try
            {
                return androidAgentGL.CallStatic<int>("getFPSTime");
            }
            catch(Exception e)
            {
                androidAgentGL = null;
            }
        }
        return -1;
    }

    int getDrawcalls()
    {
        if (androidAgentGL != null)
        {
            try
            {
                return androidAgentGL.CallStatic<int>("getDrawcalls");
            }
            catch (Exception e)
            {
                androidAgentGL = null;
            }
        }
        return -1;
    }

    int getTriangles()
    {
        if (androidAgentGL != null)
        {
            try
            {
                return androidAgentGL.CallStatic<int>("getTriangles");
            }
            catch (Exception e)
            {
                androidAgentGL = null;
            }
        }
        return -1;
    }
}
