using UnityEngine;
using System.Collections;

public class BaseBullet : MonoBehaviour
{
    public SphereCollider m_Collider;
    public GameObject m_Model;
    //public float shootSpeed = 10;
    //public float bulletMass = 0.1f;
    public int maxHitCount = 5;
    public float hitedLoss = 0.2f;
    public Vector3 gravity = Vector3.down * 10;
    public float xLimitMin = -500;
    public float xLimitMax = 500;
    public float yLimitMin = -10;
    public float yLimitMax = 100;
    public float zLimitMin = -500;
    public float zLimitMax = 500;
    [HideInInspector]
    public string playerId;
    [HideInInspector]
    public Vector3 velocity = Vector3.zero;
    [HideInInspector]
    public Vector3 angularVelocity = Vector3.zero;





    Vector3 lastVelocity = Vector3.zero;
    Vector3 lastAcceleration = Vector3.zero;
    Vector3 lastPreHitDistace = Vector3.zero;
    Vector3 lastHitDistace = Vector3.zero;
    Vector3 tempVelocity = Vector3.zero;
    RaycastHit lastHited = new RaycastHit();
    RaycastHit finalHited = new RaycastHit();
    bool hasHited = false;
    bool hasOverLimit = false;
    int hitCount = 0;

    public void Init(string playerId = "")
    {
        this.playerId = playerId;
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;
        hitCount = 0;
        hasHited = false;
        hasOverLimit = false;

        tempVelocity = Vector3.zero;
        lastVelocity = Vector3.zero;
        lastAcceleration = Vector3.zero;
        lastPreHitDistace = Vector3.zero;
        lastHitDistace = Vector3.zero;
        lastHited = new RaycastHit();
        if(m_Model!=null)m_Model.transform.localEulerAngles = Vector3.zero;
    }
 
    public void Fly(Vector3 speed)
    {
        velocity = speed;
    }

    void DealHited(RaycastHit hit)
    {
        if (hasHited || hasOverLimit) return;
        gameObject.transform.position = hit.point - (transform.forward * m_Collider.radius);
        hitCount++;
        OnHited(hit);
        if (hitCount < maxHitCount)
        {
            if (tempVelocity.magnitude > 0)
            {
                velocity = tempVelocity;
            }
            
            Vector3 reflectV = Vector3.Reflect(velocity, hit.normal);
            Vector3 cc = gameObject.transform.position;
            velocity = reflectV * (1- hitedLoss);
            transform.forward = velocity.normalized;
            if(m_Model!=null)angularVelocity += Vector3.RotateTowards(m_Model.transform.forward, hit.normal,3,3) * 20 * (1 - hitedLoss);

            tempVelocity = Vector3.zero;
            lastVelocity = Vector3.zero;
            lastAcceleration = Vector3.zero;
            lastPreHitDistace = Vector3.zero;
            lastHitDistace = Vector3.zero;
            lastHited = new RaycastHit();
            
        }
        else
        {
            hasHited = true;
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            finalHited = hit;
            OnHitedEnd(hit);
        }
    }

    void UpdateVelocity()
    {
        Vector3 v = velocity + gravity * Time.deltaTime;
        velocity = v;
        if(tempVelocity.magnitude > 0)
        {
            tempVelocity += gravity * Time.deltaTime;
        }
        if(velocity.magnitude>0)transform.forward = velocity.normalized;
    }

    void UpdatePositon()
    {
        Vector3 d = velocity * Time.deltaTime;
        transform.position += d;
        Vector3 pos = transform.position;
        if (pos.x < xLimitMin || pos.x > xLimitMax || pos.y < yLimitMin || pos.y > yLimitMax || pos.z < zLimitMin || pos.z > zLimitMax)
        {
            hasOverLimit = true;
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
            OnOverLimit();
        }
    }

    void UpdateRotation()
    {
        if (m_Model != null)
        {
            m_Model.transform.Rotate(angularVelocity,Space.Self);
        }
    }

    bool CheckHitedTarget(RaycastHit hit)
    {
        ActorPlayer player = hit.collider.gameObject.GetComponentInParent<ActorPlayer>();
        if(player == null)
        {
            return true;
        }
        return (player.playerId != playerId);
    }
    

    private void Update()
    {
        if (hasHited || hasOverLimit) return;
        UpdateVelocity();
        RaycastHit hit;
        float tPre = Time.deltaTime;
        int layerMask = (~LayerMask.GetMask("Bullet")) & (~LayerMask.GetMask("PlayerCharacter"));
        if (Physics.Raycast(transform.position,transform.forward,out hit, 100, layerMask) && CheckHitedTarget(hit))
        {
            Vector3 d = hit.point - (transform.position + transform.forward * m_Collider.radius);
            if(d.magnitude < 0.01)
            {
                DealHited(hit);
                return;
            }
            
            Vector3 dPre = velocity * tPre + 0.5f * lastAcceleration * tPre * tPre;
            float dOvered = dPre.magnitude - d.magnitude;
            if (dOvered > 0)
            {
                //Vector3 v = velocity.normalized * d.magnitude / tPre - tPre * lastAcceleration;
                //if (Vector3.Dot(v, velocity) < 0)
                {
                    DealHited(hit);
                    return;
                }
                //if (tempVelocity.magnitude == 0) tempVelocity = velocity;
                //velocity = v;
                //dPre = velocity * tPre + 0.5f * lastAcceleration * tPre * tPre;
            }
            lastPreHitDistace = dPre;
            lastHitDistace = d;
            lastHited = hit;
        }
        else
        {
            if(lastHitDistace.magnitude > 0)
            {
                //Vector3 v = velocity.normalized * lastHitDistace.magnitude / tPre - tPre * lastAcceleration;
                //if (Vector3.Dot(v, velocity) < 0)
                {
                    DealHited(lastHited);
                    return;
                }
                //if (tempVelocity.magnitude == 0) tempVelocity = velocity;
                //velocity = v;
                //Vector3 dPre = velocity * tPre + 0.5f * lastAcceleration * tPre * tPre;
                //lastPreHitDistace = dPre;
            }
        }
        //Debugger.Log(lastHitDistace.magnitude+"|"+ lastPreHitDistace.magnitude+"|"+ velocity+"|"+ lastHited.normal);
        lastAcceleration = (velocity - lastVelocity) / Time.deltaTime;
        lastVelocity = velocity;
        UpdateRotation();
        UpdatePositon();
    }

    public virtual void OnHited(RaycastHit hited)
    {
        //Debugger.Log("Hited=>"+hited.collider.name);
    }

    public virtual void OnHitedEnd(RaycastHit hited)
    {
        //Debugger.Log("HitedEnd=>"+hited.collider.name);
        NGlobal.Facade.SendNotification(Const.Notify.UpdateBulletShot, new BulletShotInfo()
        {
            bullet = this,
        });
        StartCoroutine(OnBulletDied());
        //Release();
    }

    IEnumerator OnBulletDied()
    {
        yield return new WaitForSeconds(3);
        Release();
    }

    public virtual void OnOverLimit()
    {
        //Debugger.Log("OverLimit");
        Release();
    }

    public void Release()
    {
        ObjectPoolManager.Instance.Release(name, this.gameObject);
    }

    public bool GetFinalHited(out RaycastHit hited)
    {
        hited = finalHited;
        return hasHited;
    }
}
