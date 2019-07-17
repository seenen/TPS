using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

    // Use this for initialization
    void Start () {
        DontDestroyOnLoad (this.gameObject);
        AppFacade.getInstance.startup ();
    }
    
    // Update is called once per frame
    void Update () {
    
    }
}