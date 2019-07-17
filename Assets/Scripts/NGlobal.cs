using UnityEngine;
using PureMVC.Patterns.Facade;
using PureMVC.Interfaces;
using System;

public class NGlobal : MonoBehaviour
{
    public static Facade Facade = new Facade("Facade");
    
    public static NGlobal Instance { get; private set; }
    

    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
        Time.fixedDeltaTime = Const.FrameTime;

        InitProxy();
        InitManager();
    }
    
    void InitProxy()
    {
        Facade.RegisterProxy(new GlobalProxy(GlobalProxy.NAME, new GlobalPackage()));
    }

    void InitManager()
    {
        Facade.RegisterMediator(ThreadManager.Instance);
        Facade.RegisterMediator(ObjectPoolManager.Instance);
        Facade.RegisterMediator(InputManager.Instance);
        Facade.RegisterMediator(InGameManager.Instance);
    }

    private void Start()
    {
        
    }
    
}
