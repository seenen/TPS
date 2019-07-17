using UnityEngine;
using System.Collections;
using PureMVC.Patterns;
using PureMVC.Interfaces;
using PureMVC.Patterns.Facade;


public class AppFacade : Facade,IFacade {
    public const string STARTUP = "starup";
    public const string LOGIN = "login";
    private static AppFacade _instance;
    public static AppFacade getInstance
    {
        get{ 
            if (_instance == null) {
                _instance = new AppFacade ();
            }
            return _instance;
        }
    }
    protected override void InitializeController ()
    {
        base.InitializeController ();
        RegisterCommand (STARTUP, typeof(StartupCommand));
        RegisterCommand (NotiConst.S_LOGIN, typeof(LoginCommand));
    }
    public void startup()
    {
        SendNotification (STARTUP);
    }
}