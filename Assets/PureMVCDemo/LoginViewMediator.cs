using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using PureMVC.Patterns;
using PureMVC.Interfaces;
using PureMVC.Patterns.Mediator;
using PureMVC.Patterns.Command;

public class LoginViewMediator : Mediator,IMediator {

    public const string NAME = "LoginViewMediator";

    public LoginViewMediator(LoginView _view):base(NAME,_view){
        
    }
    //需要监听的消息号
    // public override System.Collections.Generic.IList<string> ListNotificationInterests ()
    public override string[] ListNotificationInterests ()
    {
        List<string> list = new List<string>();
        list.Add (NotiConst.R_LOGIN);
        return list.ToArray();
    }
    //接收消息到消息之后处理
    public override void HandleNotification (PureMVC.Interfaces.INotification notification)
    {
        string name = notification.Name;
        object vo = notification.Body;
        switch (name) {
        case NotiConst.R_LOGIN:
                (this.ViewComponent as LoginView).receiveMessage (vo);
                break;
        }
    }
}