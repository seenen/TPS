using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public enum PlayerTeam
{
    PlayerTeam1 = 1,
    PlayerTeam2 = 2,
}

public class ActorPlayer : MonoBehaviour
{
    public GameObject m_CameraCenter;
    public GameObject m_CameraContainer;
    public GameObject m_CameraPivot;
    public GameObject m_CameraLookPoint;
    public Camera m_Camera;
    public GameObject[] m_HalfTops;
    public GameObject[] m_HalfBottoms;
    public float feetMoveAngle = 85;
    //视野转动速度
    public float lookHorizontalSpeed = 10f;
    public float lookVerticalSpeed = 10f;
    //上下观察范围
    public float lookVerticalMin = -90;
    public float lookVerticalMax = 90;
    public float moveSpeed = 5f;　　//移动速度
    public float jumpSpeed = 4.0f;
    public float gravity = 10.0f;

    public GameObject m_WeaponPoint;
    public int weaponId = -1;
    public GameObject[] m_ValidWeapon;

    public bool isPlayer = false;//player模式
    public bool isMock = false;
    public string playerId = "";//playerId
    //public PlayerTeam playerTeam;
    public BaseBodyController[] m_BodyControllers;
    public BaseWeaponController[] m_WeaponControllers;

    public Animator _animator = null;

    CharacterController characterController;
    GameObject curWeaponObj;

    Vector3 moveVelocity = Vector3.zero;
    

    float lastFeetAngle = 0;
    float curFeetAngle = 0;

    //观察变化量
    float lastRotationX;
    float rotationX;
    float rotationY;
    //OperationValue
    Vector3 playerPosition = Vector3.zero;
    float playerHorizontalLookMove = 0;
    float playerVerticalLookMove = 0;
    float playerHorizontalMove = 0;
    float playerVerticalMove = 0;
    bool playerJump = false;
    bool playerShooting = false;
    bool playerShootStart = false;
    bool playerShootEnd = false;
    bool playerChangeWeapon1 = false;
    bool playerChangeWeapon2 = false;
    bool playerChangeWeapon3 = false;

    bool hasReady = false;
    bool playerRotating;

    AnimLogicManager animLogicManager;
    

    // Use this for initialization
    void Start() 
    {
        Init();
    }

    public bool HasReadyPlay()
    {
        return hasReady;
    }

    public void OpenCamera()
    {
        m_Camera.gameObject.SetActive(true);
    }

    public void CloseCamera()
    {
        m_Camera.gameObject.SetActive(false);
    }

    Vector2 lastMoveDir = Vector2.zero;

    Vector2 _localMove = Vector2.zero;
    private void Update()
    {
        //_forceDir = InputManager.Instance.localMoveDir;
        if (_localMove != _localMoveDir)
        {
            _localMove += (_localMoveDir - _localMove) * Time.deltaTime * 6;
            _localMove.x = Mathf.Clamp(_localMove.x, -1, 1);
            _localMove.y = Mathf.Clamp(_localMove.y, -1, 1);
            if (Mathf.Abs(_localMove.x) < 0.001f)
            {
                _localMove.x = 0;
            }
            if (Mathf.Abs(_localMoveDir.y) < 0.001f)
            {
                _localMove.y = 0;
            }
        }

        if (_animator != null) _animator.SetFloat("RunDirection_x", _localMove.x);
        if (_animator != null) _animator.SetFloat("RunDirection_y", _localMove.y);


        if (!isPlayer)
        {
            UpdatePlayer(true);
        }
        animLogicManager.Update();
    }

    public void UpdatePlayerByCommandResult(ActorPlayerCommandResult result)
    {
        if (!HasReadyPlay()) return;
        // UpdateChangeWeapon(result.hasChangeWeapon, result.changeWeaponId);
        // UpdateLook(result.angleX, result.angleY, result.rotationX, result.rotationY);
        UpdateMove(result.position, result.movement, result.moveVelocity, result.horizontalMove,result.verticalMove);
        // UpdateAim(result.hasHit, result.aimDir, result.hit);
        // UpdateCamera();
        // UpdateShoot(result.shootStart, result.shooting, result.shootEnd);
    }

    public void UpdatePlayer(bool curInput)
    {
        ActorPlayerCommandInput input = BuildCommmandInput(curInput);
        ActorPlayerCommandResult result = BuildCommmandResult(input);
        UpdatePlayerByCommandResult(result);
    }
    //初始化角色
    public void Init()
    {

        lastFeetAngle = 0;
        curFeetAngle = 0;

        //观察变化量
        lastRotationX = 0;
        rotationX = 0;
        rotationY = 0;
        //OperationValue
        playerPosition = Vector3.zero;
        playerHorizontalLookMove = 0;
        playerVerticalLookMove = 0;
        playerHorizontalMove = 0;
        playerVerticalMove = 0;
        playerJump = false;
        playerShooting = false;
        playerShootStart = false;
        playerShootEnd = false;
        playerChangeWeapon1 = false;
        playerChangeWeapon2 = false;
        playerChangeWeapon3 = false;

        hasReady = false;
        playerRotating = false;

        animLogicManager = null;

        characterController = GetComponent<CharacterController>();
        animLogicManager = new AnimLogicManager(this);
        UpdateBodyController();
        UpdateWeaponController();
        OnWeaponChanged(weaponId);
        hasReady = true;
    }

    public void UpdateBodyController()
    {
        m_BodyControllers = GetComponentsInChildren<BaseBodyController>(true);
    }

    public void UpdateWeaponController()
    {
        HandController[] hands = GetComponentsInChildren<HandController>(true);
        m_WeaponControllers = GetComponentsInChildren<BaseWeaponController>(true);
        for (int j = 0; j < hands.Length; j++)
        {
            hands[j].mainBulletPoint = null;
        }
        for (int i = 0; i < m_WeaponControllers.Length; i++)
        {
            m_WeaponControllers[i].Init(playerId, m_Camera);
            for(int j = 0; j < hands.Length; j++)
            {
                if (hands[j].mainBulletPoint == null)
                {
                    hands[j].mainBulletPoint = m_WeaponControllers[i].m_BulletPoint;
                }
            }
        }
    }

    public void SetPlayerHorizontalLookMove(float move)
    {
        playerHorizontalLookMove = move;
    }

    public void SetPlayerVerticalLookMove(float move)
    {
        playerVerticalLookMove = move;
    }

    public float GetHorizontalLookMove(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return playerHorizontalLookMove;
        return Mathf.Floor(InputManager.Instance.lookDelta.x * 1000) / 1000f;
    }

    public float GetVerticalLookMove(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return playerVerticalLookMove;
        return Mathf.Floor(InputManager.Instance.lookDelta.y * 1000) / 1000f;
    }


    public void SetPlayerHorizontalMove(float move)
    {
        playerHorizontalMove = move;
    }

    public void SetPlayerVerticalMove(float move)
    {
        playerVerticalMove = move;
    }

    public void SetPlayerJump(bool jump)
    {
        playerJump = jump;
    }

    public void SetPlayerPosition(Vector3 position)
    {
        playerPosition = position;
    }

    public Vector3 GetPosition(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return transform.position;
        float x = Mathf.Floor(transform.position.x * 1000) / 1000f;
        float y = Mathf.Floor(transform.position.y * 1000) / 1000f;
        float z = Mathf.Floor(transform.position.z * 1000) / 1000f;
        return new Vector3(x,y,z);
    }

    public Vector3 GetVelocity(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return moveVelocity;
        float x = Mathf.Floor(moveVelocity.x * 1000) / 1000f;
        float y = Mathf.Floor(moveVelocity.y * 1000) / 1000f;
        float z = Mathf.Floor(moveVelocity.z * 1000) / 1000f;
        return new Vector3(x, y, z);
    }

    public float GetHorizontalMove(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return playerHorizontalMove;
        return Mathf.Floor(InputManager.Instance.localMoveDir.x * 1000) / 1000f;
    }

    public float GetVerticalMove(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return playerVerticalMove;
        return Mathf.Floor(InputManager.Instance.localMoveDir.y * 1000) / 1000f;
    }

    public bool GetJump(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return playerJump;
        return InputManager.Instance.jump;
    }

    public void SetPlayerShooting(bool shoot)
    {
        playerShooting = shoot;
    }
    public void SetPlayerShootStart(bool shoot)
    {
        playerShootStart = shoot;
    }
    public void SetPlayerShootEnd(bool shoot)
    {
        playerShootEnd = shoot;
    }

    public bool GetShooting(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return playerShooting;
        return InputManager.Instance.shooting;
    }

    public bool GetShootStart(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return playerShootStart;
        return InputManager.Instance.shootStart;
    }

    public bool GetShootEnd(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return playerShootEnd;
        return InputManager.Instance.shootEnd;
    }

    public void SetPlayerChangeWeapon1(bool changed)
    {
        playerChangeWeapon1 = changed;
    }

    public void SetPlayerChangeWeapon2(bool changed)
    {
        playerChangeWeapon2 = changed;
    }

    public void SetPlayerChangeWeapon3(bool changed)
    {
        playerChangeWeapon3 = changed;
    }

    public bool GetChangeWeapon1(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return playerChangeWeapon1;
        return Input.GetKeyDown(KeyCode.Alpha1);
    }
    public bool GetChangeWeapon2(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return playerChangeWeapon2;
        return Input.GetKeyDown(KeyCode.Alpha2);
    }
    public bool GetChangeWeapon3(bool useLocal = false)
    {
        if (isPlayer && !useLocal) return playerChangeWeapon3;
        return Input.GetKeyDown(KeyCode.Alpha3);
    }

    public void GetUpdateMove(ActorPlayerCommandInput input,out Vector3 movement,out Vector3 moveVelocity)
    {
        RaycastHit hit;
        moveVelocity = input.moveVelocity;
        if (CheckCharacterIsGround(input.position,out hit))
        {
#if UNITY_EDITOR
            moveVelocity = Quaternion.Euler(0, input.rotationX, 0) * new Vector3(input.horizontalMove, 0, input.verticalMove).normalized;
#else
            moveVelocity = Quaternion.Euler(0, input.rotationX, 0) * new Vector3(input.horizontalMove, 0, input.verticalMove);
#endif
            moveVelocity *= moveSpeed;
            if (input.jump)
                moveVelocity.y = jumpSpeed;
        }
        else
        {
            moveVelocity.y -= gravity * Const.FrameTime;
        }
        movement = moveVelocity * Const.FrameTime;
    }

    bool CheckCharacterIsGround(Vector3 position,out RaycastHit hit)
    {
        float height = characterController.height;
        float radius = characterController.radius;
        Vector3 center = characterController.center;
        Vector3 p1  = position + center + transform.up * (-height * 0.5f + radius);
        int layerMask = (~LayerMask.GetMask("Bullet")) & (~LayerMask.GetMask("PlayerMyself"));
        bool result = Physics.SphereCast(p1, radius, Vector3.down, out hit, 0.1f, layerMask);
#if UNITY_EDITOR
        if(result) DebugExtension.DebugPoint(hit.point, Color.red);
#endif
        return result;
    }
    bool isRun = false;
    Vector2 _localMoveDir = Vector2.zero;
    void UpdateMove(Vector3 position,Vector3 movement,Vector3 moveVelocity , float horizontalMove, float verticalMove)
    {
        transform.position = position;
        Move(movement);
        if (movement.x != 0 || movement.y != 0 || movement.z != 0)
        {
            if (turnOffTweener != null && turnOffTweener.active) turnOffTweener.Kill();
            transform.eulerAngles = new Vector3(0, rotationX, 0);
            if(_animator!=null) _animator.SetFloat("YawAngle", 0);
            lastFeetAngle = curFeetAngle;
            for (int i = 0; i < m_HalfTops.Length; i++)
            {
                m_HalfTops[i].transform.localEulerAngles = Vector3.zero;
            }


            for (int i = 0; i < m_BodyControllers.Length; i++)
            {
                if (m_BodyControllers[i].enabled) m_BodyControllers[i].OnMoveChanged(movement);
            }
        }
        _localMoveDir.x = horizontalMove;
        _localMoveDir.y = verticalMove;
        if (horizontalMove != 0 || verticalMove != 0)
        {
            if (animLogicManager.Add(ELogicState.Run, new CallbackLogic(null, null, null, null)))
            {
                PlayAction("Run",true);
            }
        }
        else
        {
            Idle();
        }

        this.moveVelocity = moveVelocity;
    }

    private void Move(Vector3 movement)
    {
        Physics.SyncTransforms();
        characterController.Move(movement);
    }

    void UpdateCamera()
    {
        if (!m_Camera.enabled) return;
        Vector3 cPos = m_CameraCenter.transform.position;
        Vector3 cameraPos = m_CameraContainer.transform.position;
        Vector3 cameraDir = (cameraPos - cPos);
        float castLength = cameraDir.magnitude;
        cameraDir.Normalize();

#if UNITY_EDITOR
        Debug.DrawLine(cPos, cameraPos, Color.blue);
#endif
        float radius = m_Camera.nearClipPlane * Mathf.Tan(m_Camera.fieldOfView * Mathf.Deg2Rad) + 0.01f;
        int layer = (LayerMask.GetMask("Default"));
        Vector3[] posOffsets = new Vector3[] {
            Vector3.zero,
            m_CameraContainer.transform.up * radius,
            -m_CameraContainer.transform.up * radius,
            m_CameraContainer.transform.right * radius,
            -m_CameraContainer.transform.right * radius,
        };
        bool hasHited = false;
        RaycastHit hit;
        /*for(int i = 0; i < posOffsets.Length; i++)
        {
            if (!hasHited)
            {
                Vector3 rPos = cPos + posOffsets[i];
                if (Physics.Raycast(rPos, cameraDir, out hit, castLength, layer))
                {
                    Vector3 hitDis = Vector3.Project(hit.point - rPos, cameraDir);
                    Vector3 posHit = hitDis + cPos;

                    Vector3 project = Vector3.ProjectOnPlane(cameraDir, hit.normal).normalized * Mathf.Max(0, castLength - (hitDis.magnitude));
#if UNITY_EDITOR
                    Debug.DrawLine(cPos, posHit);
                    Debug.DrawLine(posHit, posHit + project, Color.red);
#endif
                    m_CameraContainer.transform.position = posHit + project;
                    hasHited = true;
                }
            }
            else
            {
                break;
            }
        }*/


        {
            float[] radiuses = new float[] { radius, radius / 2, radius / 4 };
            for(int i = 0; i < radiuses.Length; i++)
            {
                if (!hasHited)
                {
                    radius = radiuses[i];
                    Vector3 rPos = cPos;
                    if (Physics.SphereCast(rPos, radius, cameraDir, out hit, castLength, layer))
                    {
                        Vector3 hitDis = Vector3.Project(hit.point - rPos, cameraDir);
                        Vector3 posHit = hitDis + rPos;
                        if (Vector3.Dot(posHit - cPos, cameraDir) < 0)
                        {
                            hitDis = Vector3.zero;
                            posHit = cPos;
                        }

                        Vector3 project = Vector3.ProjectOnPlane(cameraDir, hit.normal).normalized * Mathf.Max(0, castLength - (hitDis.magnitude));
#if UNITY_EDITOR
                        Debug.DrawLine(cPos, posHit);
                        Debug.DrawLine(rPos, rPos + cameraDir * radius, Color.green);
                        Debug.DrawLine(posHit, posHit + project, Color.red);
#endif
                        m_CameraContainer.transform.position = posHit;
                        hasHited = true;
                    }
                }
                else
                {
                    break;
                }
            }
            
        }

        if (!hasHited)
        {
            //m_CameraContainer.transform.localPosition = Vector3.zero;
        }
        /*
        cPos = m_CameraContainer.transform.position;
        
        float radiusRight = m_Camera.nearClipPlane * Mathf.Tan(m_Camera.fieldOfView * Mathf.Deg2Rad) + 0.01f;
        float radiusBack = m_Camera.nearClipPlane + 0.01f;
        float radiusLeft = radiusRight;
        float radiusForward = radiusBack;
        Vector3 offsetBack = Vector3.zero;
        radius = radiusBack;
        cameraDir = -m_CameraContainer.transform.forward;
        if (Physics.Raycast(cPos- cameraDir * radius, cameraDir, out hit, radius * 2, layer))
        {
#if UNITY_EDITOR
            Debug.DrawLine(hit.point, cPos - cameraDir * radius, Color.green);
#endif
            offsetBack = hit.point - cameraDir * radius - cPos;
        }

        Vector3 offsetForward = Vector3.zero;
        radius = radiusForward;
        cameraDir = m_CameraContainer.transform.forward;
        if (Physics.Raycast(cPos - cameraDir * radius, cameraDir, out hit, radius * 2, layer))
        {
#if UNITY_EDITOR
            Debug.DrawLine(hit.point, cPos - cameraDir * radius, Color.green);
#endif
            offsetForward = hit.point - cameraDir * radius - cPos;
        }

        Vector3 offsetRight = Vector3.zero;
        radius = radiusRight;
        cameraDir = m_CameraContainer.transform.right;
        if (Physics.Raycast(cPos - cameraDir * radius, cameraDir, out hit, radius * 2, layer))
        {
#if UNITY_EDITOR
            Debug.DrawLine(hit.point, cPos - cameraDir * radius, Color.green);
#endif
            offsetRight = hit.point - cameraDir * radius - cPos;
        }

        Vector3 offsetLeft = Vector3.zero;
        radius = radiusLeft;
        cameraDir = -m_CameraContainer.transform.right;
        if (Physics.Raycast(cPos - cameraDir * radius, cameraDir, out hit, radius * 2, layer))
        {
#if UNITY_EDITOR
            Debug.DrawLine(hit.point, cPos - cameraDir * radius, Color.green);
#endif
            offsetLeft = hit.point - cameraDir * radius - cPos;
        }
        m_CameraContainer.transform.position += (offsetBack.sqrMagnitude > offsetForward.sqrMagnitude ? offsetBack : offsetForward);
        m_CameraContainer.transform.position += (offsetRight.sqrMagnitude > offsetLeft.sqrMagnitude ? offsetRight : offsetLeft);
        */
    }

    public void GetUpdateLook(ActorPlayerCommandInput input, out float angleX, out float angleY, out float rotationX, out float rotationY)
    {
        angleX = input.horizontalLookMove * lookHorizontalSpeed; //水平旋转量，绕Y轴旋转
        angleY = input.verticalLookMove * lookVerticalSpeed; //垂直旋转量，绕X轴旋转
        rotationX = input.rotationX;
        rotationY = input.rotationY;
        rotationX = (rotationX + angleX + 360) % 360;
        rotationY += angleY;
        rotationY = Mathf.Clamp(rotationY, lookVerticalMin, lookVerticalMax);
    }
    Tweener turnOffTweener = null;

    float UnitAngle(float angle)
    {
        if (angle < -180)
        {
            return angle + 360;
        }
        else if (angle > 180)
        {
            return angle - 360;
        }
        return angle;
    }



    void UpdateLook(float angleX, float angleY, float rotationX, float rotationY)
    {
        bool needWholeRotate = false;
        lastRotationX = this.rotationX;
        this.rotationX = rotationX;
        this.rotationY = rotationY;
        curFeetAngle = rotationX;
        
        if (_animator != null) _animator.SetFloat("UpAngle", rotationY);
        float yawAngle = UnitAngle(curFeetAngle - lastFeetAngle);
        
        if (Mathf.Abs(yawAngle) > feetMoveAngle)
        {
            needWholeRotate = true;
            TurnOff(yawAngle<0);
            Vector3 from = transform.eulerAngles;
            float changed = 0;
            float delta = UnitAngle(curFeetAngle - lastFeetAngle);
            float actorRotate = 0;
            playerRotating = true;
            turnOffTweener = DOTween.To(() => { return actorRotate; }, (x) => { changed = x- actorRotate; actorRotate = x; }, delta, 0.5f).OnUpdate(() => {
                Vector3 rotation = new Vector3(0, changed, 0);
                transform.Rotate(rotation);
                if (_animator != null )
                {
                    _animator.SetFloat("YawAngle", _animator.GetFloat("YawAngle") - changed);
                    for (int i = 0; i < m_HalfTops.Length; i++)
                    {
                        m_HalfTops[i].transform.localEulerAngles = (new Vector3(0, _animator.GetFloat("YawAngle"), 0));
                    }
                }
            }).OnComplete(() => {
                lastFeetAngle = this.rotationX - _animator.GetFloat("YawAngle");
                playerRotating = false;
            }).OnKill(()=> {
                lastFeetAngle = this.rotationX - _animator.GetFloat("YawAngle");
                playerRotating = false;
            });
            lastFeetAngle = curFeetAngle;
        }
        if (playerRotating)//正在旋转时旋转，直接作用
        {
            transform.Rotate(new Vector3(0, angleX, 0));
        }
        if (_animator != null && !needWholeRotate && !playerRotating)
        {
            _animator.SetFloat("YawAngle", yawAngle);
            for (int i = 0; i < m_HalfTops.Length; i++)
            {
                m_HalfTops[i].transform.localEulerAngles = (new Vector3(0, _animator.GetFloat("YawAngle"), 0));
            }
        }
        
        if (m_Camera != null && m_Camera.enabled)
        {
            Vector3 dis = m_CameraPivot.transform.position - m_CameraLookPoint.transform.position;
            dis = Vector3.Project(dis, m_CameraPivot.transform.forward);
            Vector3 rotatePoint = m_CameraPivot.transform.position - dis;
            Quaternion rotate = Quaternion.LookRotation(dis) * Quaternion.Euler(rotationY,0,0);
            m_CameraContainer.transform.position = rotatePoint + rotate * Vector3.forward * dis.magnitude;
            m_CameraContainer.transform.localEulerAngles = new Vector3(-rotationY, 0, 0);
#if UNITY_EDITOR
            Debug.DrawRay(m_CameraContainer.transform.position, m_CameraContainer.transform.forward * 1000, Color.red);
#endif
            if (m_WeaponControllers.Length > 0)
            {
                Vector2 recoilAngle = m_WeaponControllers[0].GetRecoilAngle();//后坐力对相机影响
                m_Camera.transform.localEulerAngles = new Vector3(-recoilAngle.y, recoilAngle.x,0);
            }
        }
        for (int i = 0; i < m_BodyControllers.Length; i++)
        {
            if (m_BodyControllers[i].enabled) m_BodyControllers[i].OnLookChanged(angleX, angleY, rotationX, rotationY);
        }
    }

    public void GetUpdateShoot(ActorPlayerCommandInput input, out bool shooting, out bool shootStart, out bool shootEnd)
    {
        shooting = input.shooting;
        shootStart = input.shootStart;
        shootEnd = input.shootEnd;
    }

    public void GetAim(out bool hasHit, out Vector3 aimDir, out Vector3 hit)
    {
        hasHit = false;
        aimDir = Vector3.zero;
        hit = Vector3.zero;
        Vector3 pos = Vector3.zero;
        if (m_WeaponControllers.Length > 0)
        {
            RaycastHit hited;
            hasHit = m_WeaponControllers[0].GetHitPosition(out pos, out aimDir, out hited);
            pos = new Vector3(Mathf.Floor(pos.x * 1000) / 1000f, Mathf.Floor(pos.y * 1000) / 1000f, Mathf.Floor(pos.z * 1000) / 1000f);
            if (hasHit)
            {
                hit = hited.point;
                //hit = new Vector3(Mathf.Floor(hit.x * 1000) / 1000f, Mathf.Floor(hit.y * 1000) / 1000f, Mathf.Floor(hit.z * 1000) / 1000f);
                aimDir = hit - pos;
            }
            aimDir = new Vector3(Mathf.Floor(aimDir.x * 1000) / 1000f, Mathf.Floor(aimDir.y * 1000) / 1000f, Mathf.Floor(aimDir.z * 1000) / 1000f);
        }
    }


    void UpdateAim(bool hasHit, Vector3 aimDir, Vector3 hit)
    {
        for (int i = 0; i < m_BodyControllers.Length; i++)
        {
            if (m_BodyControllers[i].enabled) m_BodyControllers[i].OnAim(hasHit, aimDir, hit);
        }
        for (int i = 0; i < m_WeaponControllers.Length; i++)
        {
            if (m_WeaponControllers[i].enabled) m_WeaponControllers[i].OnAim(hasHit, aimDir, hit);
        }
    }

    void UpdateShoot(bool shooting, bool shootStart, bool shootEnd)
    {

        if (shootStart)
        {
            for (int i = 0; i < m_BodyControllers.Length; i++)
            {
                if (m_BodyControllers[i].enabled) m_BodyControllers[i].OnShootStart();
            }
            for (int i = 0; i < m_WeaponControllers.Length; i++)
            {
                if (m_WeaponControllers[i].enabled) m_WeaponControllers[i].OnShootStart();
            }
        }

        if (shooting)
        {
            for (int i = 0; i < m_WeaponControllers.Length; i++)
            {
                if (m_WeaponControllers[i].enabled) m_WeaponControllers[i].OnShooting();
            }
        }

        if (shootEnd)
        {
            for (int i = 0; i < m_BodyControllers.Length; i++)
            {
                if (m_BodyControllers[i].enabled) m_BodyControllers[i].OnShootEnd();
            }
            for (int i = 0; i < m_WeaponControllers.Length; i++)
            {
                if (m_WeaponControllers[i].enabled) m_WeaponControllers[i].OnShootEnd();
            }
            
        }

        if(shootStart || shooting)
        {
            if(animLogicManager.Add(ELogicState.Attack, null, true))
                PlayActionBool("Fire",true);
        }
        else
        {
            animLogicManager.ClearState(ELogicState.Attack);
            PlayActionBool("Fire", false);
        }
    }

    public void GetUpdateChangeWeapon(ActorPlayerCommandInput input, out bool hasChanged,out int weaponId)
    {
        hasChanged = false;
        weaponId = -1;
        bool weapon1 = input.changeWeapon1;
        bool weapon2 = input.changeWeapon2;
        bool weapon3 = input.changeWeapon3;
        if (weapon1 && weaponId != 0)
        {
            weaponId = 0;
            hasChanged = true;
        }
        if (weapon2 && weaponId != 1)
        {
            weaponId = 1;
            hasChanged = true;
        }
        if (weapon3 && weaponId != 2)
        {
            weaponId = 2;
            hasChanged = true;
        }
    }

    void UpdateChangeWeapon(bool hasChanged,int weaponId)
    {
        if (hasChanged) OnWeaponChanged(weaponId);
    }

    public void OnWeaponChanged(int weaponId)
    {
        if (curWeaponObj != null)
        {
            ObjectPoolManager.Instance.Release(curWeaponObj.name, curWeaponObj);
        }
        curWeaponObj = GetWeaponById(weaponId);
        if(curWeaponObj!=null)curWeaponObj.SetActive(true);
        UpdateWeaponController();
    }

    GameObject GetWeaponById(int weaponId)
    {
        if (weaponId < 0 || weaponId >= m_ValidWeapon.Length) return null;
        ObjectPoolManager.Instance.Register(m_ValidWeapon[weaponId].name, m_ValidWeapon[weaponId],0,5);
        GameObject weapon = ObjectPoolManager.Instance.Instantiate(m_ValidWeapon[weaponId].name);
        weapon.transform.parent = m_WeaponPoint.transform;
        weapon.transform.localScale = Vector3.one;
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.Euler(Vector3.zero);
        return weapon;
    }

    public ActorPlayerCommandInput BuildCommmandInput(bool curInput)
    {
        //todo
        ActorPlayerCommandInput input = new ActorPlayerCommandInput();
        input.position = GetPosition(curInput);
        input.moveVelocity = GetVelocity(curInput);
        input.horizontalMove = GetHorizontalMove(curInput);
        input.verticalMove = GetVerticalMove(curInput);
        input.jump = GetJump(curInput);
        input.horizontalLookMove = GetHorizontalLookMove(curInput);
        input.verticalLookMove = GetVerticalLookMove(curInput);
        input.rotationX = Mathf.Floor(rotationX * 1000) / 1000f;
        input.rotationY = Mathf.Floor(rotationY * 1000) / 1000f;
        input.changeWeapon1 = GetChangeWeapon1(curInput);
        input.changeWeapon2 = GetChangeWeapon2(curInput);
        input.changeWeapon3 = GetChangeWeapon3(curInput);
        input.shooting = GetShooting(curInput);
        input.shootStart = GetShootStart(curInput);
        input.shootEnd = GetShootEnd(curInput);
        bool hasHit;
        Vector3 aimDir;
        Vector3 hit;
        GetAim(out hasHit, out aimDir, out hit);
        input.hasHit = hasHit;
        input.aimDir = aimDir;
        input.hit = hit;
        return input;
    }

    public ActorPlayerCommandResult BuildCommmandResult(ActorPlayerCommandInput input)
    {
        ActorPlayerCommandResult result = new ActorPlayerCommandResult();
        bool hasChangeWeapon;
        int changeWeaponId;
        GetUpdateChangeWeapon(input,out hasChangeWeapon, out changeWeaponId);
        result.hasChangeWeapon = hasChangeWeapon;
        result.changeWeaponId = changeWeaponId;
        result.horizontalMove = input.horizontalMove;
        result.verticalMove = input.verticalMove;
        Vector3 movement;
        Vector3 moveVelocity;
        GetUpdateMove(input,out movement,out moveVelocity);
        result.movement = movement;
        result.position = input.position;
        result.moveVelocity = moveVelocity;
        

        float angleX;
        float angleY;
        float rotationX;
        float rotationY;
        GetUpdateLook(input,out angleX, out angleY, out rotationX, out rotationY);
        result.angleX = angleX;
        result.angleY = angleY;
        result.rotationX = rotationX;
        result.rotationY = rotationY;
        
        bool shootStart;
        bool shooting;
        bool shootEnd;
        GetUpdateShoot(input,out shootStart, out shooting, out shootEnd);
        result.shootStart = shootStart;
        result.shooting = shooting;
        result.shootEnd = shootEnd;

        result.hasHit = input.hasHit;
        result.aimDir = input.aimDir;
        result.hit = input.hit;

        return result;
    }

    public void Release()
    {
        hasReady = false;
    }

    #region 动画相关
    Dictionary<int, string> _lastActionDic = new Dictionary<int, string>();
    

    public void PlayAction(string trigger, bool allowRepeat = false)
    {
        if (_animator == null) return;
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

    public void PlayActionBool(string booltrigger, bool boolset)
    {
        if (_animator == null) return;
        _animator.SetBool(booltrigger, boolset);
    }

    public bool TurnOff(bool isLeft)
    {
        if (turnOffTweener != null && turnOffTweener.active)
        {
            turnOffTweener.Kill();
        }
        if (animLogicManager.Add(ELogicState.TurnOff, new CallbackLogic(0.667f,
            (CallbackEvent evt) =>
            {
                animLogicManager.ClearState(ELogicState.TurnOff);
                if(turnOffTweener != null)
                {
                    turnOffTweener.Kill();
                }
                Idle();
            },
            (CallbackBreakEvent evt) =>
            {
                animLogicManager.ClearState(ELogicState.TurnOff);
                if (turnOffTweener != null)
                {
                    turnOffTweener.Kill();
                }
            })
            ))
        {
            if(isLeft)
            {
                PlayAction("TurnOffLeft");
            }
            else
            {
                PlayAction("TurnOffRight");
            }
            
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool Idle()
    {
        if (animLogicManager.Add(ELogicState.Idle))
        {
            PlayAction("Stand");
            return true;
        }
        return false;
    }
    #endregion
}
