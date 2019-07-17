using UnityEngine;
using System.Collections;

public class BaseBodyController : MonoBehaviour
{
    public GameObject m_WholeBody;
    public virtual void OnLookChanged(float deltaAngleX, float deltaAngleY, float allAngleXChanged, float allAngleYChanged)
    {

    }

    public virtual void OnAim(bool hasHit, Vector3 aimDir, Vector3 hit)
    {

    }

    public virtual void OnMoveChanged(Vector3 movement)
    {

    }

    public virtual void OnShootStart()
    {
        
    }

    public virtual void OnShootEnd()
    {

    }
}
