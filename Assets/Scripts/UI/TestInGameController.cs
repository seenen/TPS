using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class TestInGameController : MonoBehaviour
{
    public Slider lookSensitivity;
    // Use this for initialization
    void Start()
    {
        lookSensitivity.onValueChanged.AddListener(delegate (float value)
        {
            InGameManager.Instance.lookSensitivity = value;
        });
    }
    
    void FixedUpdate()
    {
        if (!InputManager.Instance.shooting && InputManager.Instance.shootEnd)
        {
            InputManager.Instance.shootEnd = false;
            Debugger.Log("ShootEnd!!!");
        }
    }

    public void OnJumpDown(BaseEventData eventData)
    {
        InputManager.Instance.jump = true;
    }

    public void OnJumpUp(BaseEventData eventData)
    {
        InputManager.Instance.jump = false;
    }

    public void OnShootDown(BaseEventData eventData)
    {
        InputManager.Instance.shootStart = true;
        InputManager.Instance.shooting = true;
        InputManager.Instance.shootEnd = false;
    }

    public void OnShootUp(BaseEventData eventData)
    {
        InputManager.Instance.shootStart = false;
        InputManager.Instance.shooting = false;
        InputManager.Instance.shootEnd = true;
    }

}
