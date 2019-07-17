using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class InputManager : BaseManager
{
    static InputManager m_Instance;
    public new static string NAME = "InputManager";
    public new static InputManager Instance
    {
        get
        {
            if (m_Instance == null) m_Instance = GetInstance<InputManager>(NAME);
            return m_Instance;
        }
    }


    Camera _camera;

    struct TouchPoint
    {
        public Vector2 _beginPos;
        public Vector2 _lastPos;
        public int _beginPartition;
    }

    Dictionary<int, TouchPoint> _touchPoints = new Dictionary<int, TouchPoint>();
    
    //模型移动向量
    public Vector2 localMoveDir = Vector2.zero;
    public Vector2 lookDelta = Vector2.zero;
    public bool jump = false;
    public bool shootStart = false;
    public bool shooting = false;
    public bool shootEnd = false;
    public bool reload = false;

    
    public void SetMainCamera(Camera camera)
    {
        _camera = camera;
    }

    void OnStandAlone()
    {
        lookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        if (Input.GetKey(KeyCode.A))
        {
            localMoveDir.x = -1;
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            localMoveDir.x = 0;
        }

        if (Input.GetKey(KeyCode.D))
        {
            localMoveDir.x = 1;
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            localMoveDir.x = 0;
        }

        if (Input.GetKey(KeyCode.W))
        {
            localMoveDir.y = 1;
        }
        else if (Input.GetKeyUp(KeyCode.W))
        {
            localMoveDir.y = 0;
        }

        if (Input.GetKey(KeyCode.S))
        {
            localMoveDir.y = -1;
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            localMoveDir.y = 0;
        }
        jump = Input.GetButton("Jump");
        shootStart = Input.GetButtonDown("Fire1");
        shooting = Input.GetButton("Fire1");
        shootEnd = Input.GetButtonUp("Fire1");
        reload = Input.GetKeyDown(KeyCode.R);
    }

    void OnMobile()
    {
        if (InGameManager.Instance.isGameStart)
        {
            for (int i = 0; i < Input.touches.Length; ++i)
            {
                Touch touch = Input.touches[i];
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        {
                            var newPoint = new TouchPoint();
                            newPoint._beginPos = touch.position;
                            newPoint._lastPos = touch.position;
                            newPoint._beginPartition = (newPoint._beginPos.x < _camera.pixelWidth * 0.5f) ? 0 : 1;
                            _touchPoints[touch.fingerId] = newPoint;
                            break;
                        }
                    case TouchPhase.Moved:
                        {
                            if (!_touchPoints.ContainsKey(touch.fingerId))
                            {
                                break;
                            }
                            var point = _touchPoints[touch.fingerId];
                            bool walkOrLook = point._beginPartition == 0;
                            var dir = touch.position - point._beginPos;
                            var len = dir.magnitude;
                            var movethrehold = 10f;

                            if (walkOrLook)
                            {
                                if (len < movethrehold)
                                {
                                    localMoveDir = Vector2.zero;
                                }
                                else
                                {
                                    localMoveDir = dir.normalized;
                                }
                            }
                            else
                            {
                                var delta = touch.position - point._lastPos;
                                Vector2 deltaRatio = new Vector3(delta.x / _camera.pixelWidth, delta.y / _camera.pixelHeight);
                                Vector2 fov = new Vector2(_camera.fieldOfView * _camera.aspect, _camera.fieldOfView);

                                if (len < movethrehold)
                                {
                                    lookDelta = Vector2.zero;
                                }
                                else
                                {
                                    //float speedthrehold = Mathf.Max(1, len / 100f);
                                    lookDelta = Vector2.Scale(fov, deltaRatio * InGameManager.Instance.lookSensitivity);
                                }
                            }

                            point._lastPos = touch.position;
                            _touchPoints[touch.fingerId] = point;
                            break;
                        }
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        {
                            if (!_touchPoints.ContainsKey(touch.fingerId))
                            {
                                break;
                            }
                            bool walkOrLook = _touchPoints[touch.fingerId]._beginPartition == 0;
                            if (walkOrLook)
                            {
                                localMoveDir = Vector2.zero;
                            }
                            else
                            {
                                lookDelta = Vector2.zero;
                            }
                            _touchPoints.Remove(touch.fingerId);
                            break;
                        }
                    case TouchPhase.Stationary:
                        {
                            if (!_touchPoints.ContainsKey(touch.fingerId))
                            {
                                break;
                            }
                            bool walkOrLook = _touchPoints[touch.fingerId]._beginPartition == 0;
                            if (walkOrLook)
                            {
                            }
                            else
                            {
                                lookDelta = Vector2.zero;
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (_camera == null)
        {
            return;
        }
#if UNITY_EDITOR
        OnStandAlone();
#elif UNITY_STANDALONE
        OnStandAlone();
#else
        OnMobile();
#endif
    }


}
