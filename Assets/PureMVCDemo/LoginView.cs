using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginView : MonoBehaviour
{
    // Start is called before the first frame update
void Start () {
//注册mediator
        AppFacade.getInstance.RegisterMediator (new LoginViewMediator (this));
    }

void OnDestory(){
        AppFacade.getInstance.RemoveMediator (LoginViewMediator.NAME);
    }
}
