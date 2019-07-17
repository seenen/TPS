using System.Collections.Generic;
using UnityEngine;

public enum WeaponShootType
{
    Continuous = 1,//连射
    Single = 2,//单发射击
    ForceSingle = 3,//蓄力单射
    MultiSegment = 4,//多段射击
}

public class BaseWeaponController : MonoBehaviour
{
    public const float recoilAngleStep = 0.2f;//一单位后坐力系数角度
    public const float scatterRStep = 0.005f;//一单位散射半径系数半径值

    public GameObject m_BulletPoint;
    public GameObject m_BulletPrefab;
    public LineRenderer m_TrailLine;
    public bool showTrail = false;

    public int shootWarmUpTimeMs = 0; //射击预热时间
    public int shootTimeGapMs = 20;//射速ms
    public float shootBulletSpeed = 20;//子弹射出速度
    public int damage = 0;//单发子弹伤害
    public int clipMax = 30;//子弹弹夹

    public int horizontalRecoilIndex = 0;//出现水平后坐力连射
    public float horizontalRecoilKMin = 0;//水平后坐力初始值
    public float horizontalRecoilKMax = 0;//水平后坐力最大
    public float horizontalRecoilKStep = 0;//水平后坐力增量

    public int verticalRecoilIndex = 0;//出现垂直后坐力连射
    public float verticalRecoilKMin = 0;//垂直后坐力初始值
    public float verticalRecoilKMax = 0;//垂直后坐力最大
    public float verticalRecoilKStep = 0;//垂直后坐力增量

    
    public float recoilHorizontalMax = 2f;//水平后坐最大角度
    public float recoilVerticalMax = 5f;//垂直后座抬枪最大角度
    public float recoilRecoveryVelocity = 3f;//后坐力恢复速度

    public WeaponShootType shootType = WeaponShootType.Continuous;
    //Continuous
    public int scatterIndex = 0;//第几次连射开始散射
    public float scatterKMin = 0;//最小散射系数值
    public float scatterKMax = 0;//最大散射系数值
    public float scatterKStep = 0;//散射系数增幅

    

    public int scatterRecoveryTimeGapMs = 0;//散射恢复时间

    //ForceSingle
    public int forceMaxTimeMs = 0;//最大蓄力效果时间
    public float forceMaxBulletSpeedUp = 0;//最大蓄力初速度增量
    public int forceMaxDamage = 0;//最大蓄力伤害增量
    //MultiSegment
    public int multiSegmentTimeGapMs = 0;//多段射击间隔时间ms
    public int multiSegmentCount = 0;//多段射每段次数

   


    [HideInInspector]
    public Camera playerCamera;
    [HideInInspector]
    public bool hasHitTarget = false;
    [HideInInspector]
    public Vector3 hitTarget = Vector3.zero;
    [HideInInspector]
    public string playerId = "";


    [HideInInspector]
    public Vector3 hitDir = Vector3.zero;

    Vector3 lastHitTargetPoint = Vector3.zero;

    protected float lastShootTime = 0;
    

    System.Random random;

    bool isShooting = false;
    int curBulletIndex = 0;

    int clipRemain = 0;
    
    float recoilVertical = 0;
    float recoilHorizontal = 0;
    float recoilVerticalK = 0;
    float recoilHorizontalK = 0;

    float recoilRecoveryTime = 0;

    float recoilHRecoveryV = 0;
    float recoilVRecoveryV = 0;

    float scatterK = 0;
    float scatterR = 0;

    bool hasWarmUp = false;
    float startWarmUpTime = 0;

    private void Start()
    {

    }

    public virtual void Init(string playerId,Camera playerCamera)
    {
        this.playerId = playerId;
        this.playerCamera = playerCamera;
        clipRemain = clipMax;
        UpdateClipInfo();
    }

    void UpdateClipInfo()
    {
        NGlobal.Facade.SendNotification(Const.Notify.UpdateWeaponClip, new WeaponClipInfo
        {
            playerId = playerId,
            curClip = Mathf.Max(0, clipRemain),
            clipMax = clipMax
        });
    }

    void DebugRayCast()
    {
        if (hasHitTarget)
        {
            Debug.DrawRay(m_BulletPoint.transform.position, (hitTarget - m_BulletPoint.transform.position), Color.green);
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        DebugRayCast();
#endif
        if (showTrail)
        {
            DrawTrail();
        }
    }

    private void FixedUpdate()
    {
        DoRecoilRecovery();
    }


    void DoRecoilRecovery()
    {
        if (isShooting) return;
        float dH = (recoilHorizontal > 0 ? -recoilRecoveryVelocity : recoilRecoveryVelocity) * recoilAngleStep;
        float dV = (recoilVertical > 0 ? -recoilRecoveryVelocity : recoilRecoveryVelocity) * recoilAngleStep;
        if (Mathf.Abs(recoilVertical) < Mathf.Abs(dV))
        {
            recoilVertical = 0;
        }
        else
        {
            recoilVertical += dV;
        }
        if (Mathf.Abs(recoilHorizontal) < Mathf.Abs(dH))
        {
            recoilHorizontal = 0;
        }
        else
        {
            recoilHorizontal += dH;
        }
    }
    

    void DrawTrail()
    {
        int layerMask = (~LayerMask.GetMask("Bullet")) & (~LayerMask.GetMask("PlayerMyself")) & (~LayerMask.GetMask("PlayerCharacter"));
        Vector3 hit = hitTarget;
        Vector3 dir = hitDir;
        if (hasHitTarget)
        {
            if (m_TrailLine != null)
            {
                //if(m_TrailLine.gameObject.activeSelf && (lastHitTargetPoint - hit).magnitude < 0.001) return;
                int maxStep = 200;
                float stepWidth = Time.deltaTime * 3;
                List<Vector3> points = new List<Vector3>();
                BaseBullet bullet = m_BulletPrefab.GetComponent<BaseBullet>();
                Vector3 pos = m_BulletPoint.transform.position;

                points.Add(pos);
                Vector3 v = (hit- pos).normalized * shootBulletSpeed;
                if (Vector3.Dot(v,m_BulletPoint.transform.forward)<0)
                {
                    m_TrailLine.gameObject.SetActive(false);
                    return;
                }
                for (int i = 0; i < maxStep; i++)
                {
                    
                    RaycastHit hited;
                    
                    if (Physics.Raycast(pos, v.normalized, out hited, 100, layerMask))
                    {
                        Vector3 dis = hited.point - pos;
                        if(dis.magnitude < v.magnitude)
                        {
                            maxStep = i + 2;
                            if(dis.magnitude < 1)
                            {
                                points.Add(hited.point);
                                break;
                            }
                        }
                    }
                    v += bullet.gravity * stepWidth;
                    pos += v * stepWidth;
                    points.Add(pos);
                }
                m_TrailLine.startWidth = 0.05f;
                m_TrailLine.endWidth = 0.05f;
                m_TrailLine.positionCount = points.Count;
                m_TrailLine.SetPositions(points.ToArray());
                m_TrailLine.gameObject.SetActive(true);
                lastHitTargetPoint = hit;
            }
        }
        else
        {
            if (m_TrailLine != null) m_TrailLine.gameObject.SetActive(false);
        }
    }

    public virtual void OnAim(bool hasHit, Vector3 aimDir, Vector3 hit)
    {
        hasHitTarget = hasHit;
        hitDir = aimDir;
        hitTarget = hit;
    }

    public virtual void OnShootStart()
    {
        if (shootType != WeaponShootType.ForceSingle)
        {
            ShootBullet();
        }
        else
        {
            lastShootTime = Time.time;
        }
    }

    public virtual void OnShooting()
    {
        if (shootType == WeaponShootType.Continuous)
        {
            ShootBullet();
        }
    }

    public virtual void OnShootEnd()
    {
        isShooting = false;
        hasWarmUp = false;
        startWarmUpTime = -1;
        OnContinuousShootEnd();
    }

    public virtual void OnReload()
    {
        clipRemain = clipMax;
    }

    public virtual void OnContinuousShootEnd()
    {
        curBulletIndex = 0;
        //recoilHorizontal = 0;
        //recoilVertical = 0;
        lastShootTime = 0;
        Debugger.Log("ContinuousShootEnd");
    }

    public GameObject GetBullet()
    {
        ObjectPoolManager.Instance.Register(m_BulletPrefab.name, m_BulletPrefab, 0, 1000);
        GameObject go = ObjectPoolManager.Instance.Instantiate(m_BulletPrefab.name);
        go.transform.parent = null;
        go.transform.position = m_BulletPoint.transform.position;// + recoilHorizontal * m_BulletPoint.transform.right + recoilVertical * m_BulletPoint.transform.up;
        go.transform.rotation = m_BulletPoint.transform.rotation;
        go.transform.localScale = Vector3.one;
        return go;
    }

    public bool GetHitPosition(out Vector3 aimPos,out Vector3 dir,out RaycastHit hit)
    {
        Camera mainCamera = playerCamera;
        hit = new RaycastHit();
        dir = mainCamera.transform.forward;
        Vector3 pos = mainCamera.transform.position;
        aimPos = m_BulletPoint.transform.position;
        Vector3 fixedV = Vector3.ProjectOnPlane(pos - transform.position, mainCamera.transform.up);
        fixedV = Vector3.Project(fixedV, dir);
        pos -= fixedV;
#if UNITY_EDITOR
        Debug.DrawLine(pos, pos + mainCamera.transform.forward);
#endif
        int layerMask = (~LayerMask.GetMask("Bullet")) & (~LayerMask.GetMask("PlayerMyself")) & (~LayerMask.GetMask("PlayerCharacter"));
        if (Physics.Raycast(pos, mainCamera.transform.forward, out hit, 1000, layerMask))
        {
            dir = (hit.point - m_BulletPoint.transform.position).normalized;
#if UNITY_EDITOR
            Debug.DrawLine(hit.point, hit.point + mainCamera.transform.up, Color.yellow);
#endif
            return true;
        }
        return false;
    }

    public virtual void ShootBullet()
    {
        if (clipRemain <= 0)
        {
            isShooting = false;
            return;
        }
        StartWarmUp();
        if (!HasWarmUp()) return;

        float time = Time.time;
        if ((time - lastShootTime) * 1000 > shootTimeGapMs)
        {
            lastShootTime = time;
            isShooting = true;
        }
        else
        {
            return;
        }

        curBulletIndex++;
        
        DoRecoil();
        GameObject bulletObj = GetBullet();
        bulletObj.SetActive(true);
        BaseBullet bullet = bulletObj.GetComponent<BaseBullet>();
        bullet.Init(playerId);

        Vector3 scatterDir = GetBulletScatter();
        Quaternion rotation = Quaternion.LookRotation(hitDir) * Quaternion.LookRotation(scatterDir);
        
        Vector3 flyDir = rotation * Vector3.forward;
        /*Debug.Log(hitDir + "|" + (Quaternion.LookRotation(hitDir) * Vector3.forward) 
            + "|" + (Quaternion.LookRotation(scatterDir) * hitDir)+"|"+ (Quaternion.LookRotation(scatterDir)*(Quaternion.LookRotation(hitDir) * Vector3.forward)) 
            + "|" + (Quaternion.LookRotation(hitDir) * (Quaternion.LookRotation(scatterDir) * Vector3.forward)) + "|" + flyDir);*/
        bullet.Fly(flyDir.normalized * shootBulletSpeed);

        clipRemain--;
        UpdateClipInfo();
    }

    void UpdateRandom()
    {
        int seed = Mathf.FloorToInt(hitDir.x * 1000) + Mathf.FloorToInt(hitDir.y * 1000) + Mathf.FloorToInt(hitDir.z * 1000);
        random = new System.Random(seed);
    }

    void DoRecoil()
    {
        UpdateRandom();
        if (curBulletIndex <= 1)
        {
            recoilHorizontalK = horizontalRecoilKMin;
            recoilVerticalK = verticalRecoilKMin;
        }
        if (curBulletIndex >= horizontalRecoilIndex && horizontalRecoilIndex > 0)
        {
            recoilHorizontalK += horizontalRecoilKStep;
            if (recoilHorizontalK > horizontalRecoilKMax)
            {
                recoilHorizontalK = horizontalRecoilKMax;
            }
            int directX = (random.NextDouble() < 0.5) ? -1 : 1;
            float moveX = directX * recoilHorizontalK * recoilAngleStep * (float)random.NextDouble();
            if (Mathf.Abs(recoilHorizontal + moveX) > recoilHorizontalMax)
            {
                if (Mathf.Abs(recoilHorizontal - moveX) < recoilHorizontalMax){
                    recoilHorizontal -= moveX;
                }
            }
            else
            {
                recoilHorizontal += moveX;
            }
        }


        if (curBulletIndex >= verticalRecoilIndex && verticalRecoilIndex > 0)
        {
            recoilVerticalK += verticalRecoilKStep;
            if (recoilVerticalK > verticalRecoilKMax)
            {
                recoilVerticalK = verticalRecoilKMax;
            }
            int directY = 1;// (random.NextDouble() < 0.5) ? -1 : 1;
            float moveY = directY * recoilVerticalK * recoilAngleStep * (float)random.NextDouble();

            if (Mathf.Abs(recoilVertical + moveY) < recoilVerticalMax)
            {
                recoilVertical += moveY;
            }
        }
    }

    public Vector3 GetBulletScatter()
    {
        UpdateRandom();
        if (curBulletIndex <= 1)
        {
            scatterK = scatterKMin;
            scatterR = 0;
        }
        if (curBulletIndex >= scatterIndex && scatterIndex > 0)
        {
            scatterK += scatterKStep;
            if (scatterK > scatterKMax) scatterK = scatterKMax;
            scatterR = scatterK * scatterRStep;
            float r = scatterR * (float)random.NextDouble();
            float angle = (float)random.NextDouble() * Mathf.PI * 2;
            return new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 1).normalized;
        }
        return Vector3.forward;
    }

    public Vector2 GetRecoilAngle()
    {
        return new Vector2(recoilHorizontal, recoilVertical);
    }
    
    void StartWarmUp()
    {
        if (startWarmUpTime <= 0)
        {
            startWarmUpTime = Time.time;
            Debugger.Log("Start WarmUp");
        }
    }

    public bool HasWarmUp()
    {
        float time = Time.time;
        if (startWarmUpTime > 0 && (time - startWarmUpTime) * 1000 >= shootWarmUpTimeMs)
        {
            if (!hasWarmUp)
            {
                Debugger.Log("WarmUp Completed!!!!");
            }
            hasWarmUp = true;
            return true;
        }
        return false;
    }
}
