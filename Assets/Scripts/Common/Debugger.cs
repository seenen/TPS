using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

public class Debugger
{
    static string getTimeString()
    {
        string time = DateTime.Now.TimeOfDay.ToString();
        if (time.Length < 12) return time;
        return time.Substring(0, 12);
    }
    public static void Log(object msg)
    {
#if OPEN_DEBUG
            Debug.Log(getTimeString() + ": " + msg);
#endif
    }
    public static void LogWarning(object msg)
    {
#if OPEN_DEBUG || AUTOTEST_DEBUG
            Debug.LogWarning(getTimeString() + ": " +msg);
#endif
    }
    public static void LogError(object msg)
    {
        Debug.LogError(getTimeString() + ": " + msg);
    }
    
    public static void LogException(Exception exception)
    {
        Debug.LogException(exception);
    }
}