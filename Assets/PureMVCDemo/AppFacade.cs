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
                _instance = new AppFacade ("AppFacade");
            }
            return _instance;
        }
    }

    public AppFacade(string key) : base(key)
    {

    }
    protected override void InitializeController ()
    {
        base.InitializeController ();
        RegisterCommand (STARTUP, () => new StartupCommand());
        RegisterCommand (NotiConst.S_LOGIN, () => new LoginCommand());
    }
    public void startup()
    {
        SendNotification (STARTUP);
    }
}