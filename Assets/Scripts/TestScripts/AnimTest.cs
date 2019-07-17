using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimTest : MonoBehaviour
{
    public Camera _camera;
    public float speed = 6.0F;
    public float jumpSpeed = 8.0F;
    public float gravity = 20.0F;
    private Vector3 moveDirection = Vector3.zero;
    CharacterController controller;
    public Animator _animator;
    //模型移动向量
    Vector2 _localMoveDir = Vector2.zero;


    //视野转动速度
    float speedX = 10f;
    float speedY = 10f;
    //上下观察范围
    float minY = -60;
    float maxY = 60;
    //观察变化量
    float rotationX;
    float rotationY;

    Vector3 _rotation = Vector3.zero;
    public float _camRate;
    public float CamRateA;
    public float CamRateMin;
    public float CamRateMax;

    float lookx = 0f;
    float looky = 0f;
    void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
        PlayAction("Stand");
        _rotation = transform.eulerAngles;
        _camRate = 1.2f;
        CamRateA = 6;
        CamRateMin = 1.2f;
        CamRateMax = 2;
    }
    void Update()
    {
        float movex = Input.GetAxis("Horizontal");
        float movey = Input.GetAxis("Vertical");
       
        //CharacterController controller = GetComponent<CharacterController>();
        if (controller.isGrounded)
        {
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed;
            if (Input.GetButton("Jump"))
                moveDirection.y = jumpSpeed;
        }
        moveDirection.y -= gravity* Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);

        if (Input.GetKey(KeyCode.A))
        {
            _localMoveDir.x = -1;
        }

        if (Input.GetKey(KeyCode.D))
        {
            _localMoveDir.x = 1;
        }

        if (!Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A))
        {
            _localMoveDir.x = 0;
        }

        if (Input.GetKey(KeyCode.W))
        {
            _localMoveDir.y = 1;
        }

        if (Input.GetKey(KeyCode.S))
        {
            _localMoveDir.y = -1;
        }

        if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
        {
            _localMoveDir.y = 0;
        }
        _animator.SetFloat("RunDirection_x", movex);
        _animator.SetFloat("RunDirection_y", movey);
        if (_localMoveDir != Vector2.zero)
        {
            PlayAction("Run");
        }
        else
        {
            PlayAction("Stand");
        }

        float deltaX = Input.GetAxis("Mouse X");
        float deltaY = -Input.GetAxis("Mouse Y");
        lookx += deltaX;
        looky += deltaY;
        if (lookx >= 90)
        {
            lookx = 90;
        }
        if (lookx <= -90)
        {
            lookx = -90;
        }
        if (looky >= 55)
        {
            looky = 55;
        }
        if (looky <= -55)
        {
            looky = -55;
        }
        _animator.SetFloat("UpAngle", -looky);
        //_animator.SetFloat("YawAngle", -lookx);

        LookAround(deltaX, deltaY);
        _camera.transform.localEulerAngles = _rotation;
    }

    Dictionary<int, string> _lastActionDic = new Dictionary<int, string>();

    public void PlayAction(string trigger, bool allowRepeat = false)
    {
        int stateId = Animator.StringToHash(trigger);
        bool bTrigger = false;
        for (int i = 0; i < _animator.layerCount; i++)
        {
            bool bState = _animator.HasState(i, stateId);
            if (bState)
            {

                if (_lastActionDic.ContainsKey(i))
                {
                    if (_lastActionDic[i] == trigger && !allowRepeat)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(_lastActionDic[i]))
                    {
                        _animator.ResetTrigger(_lastActionDic[i]);
                    }
                }

                _lastActionDic[i] = trigger;
                bTrigger = true;
            }
        }
        if (bTrigger)
        {
            _animator.SetTrigger(trigger);
        }
    }

    public void LookAround(float inDeltaX, float inDeltaY)
    {
        float yRot = _rotation.y + inDeltaX * _camRate;
        float xRot = _rotation.x + inDeltaY * _camRate;
        _rotation.x = xRot;
        _rotation.y = yRot;
        _camRate = Mathf.Clamp(_camRate + (Mathf.Abs(inDeltaX) + Mathf.Abs(inDeltaY)) * 0.01f * CamRateA, CamRateMin, CamRateMax);
    }
}
