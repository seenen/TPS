using UnityEngine;
using System.Collections;

public class HandController : BaseBodyController
{
    public Camera m_Camera;
    [HideInInspector]
    public bool hasHitTarget = false;
    [HideInInspector]
    public RaycastHit hitTarget = new RaycastHit();
    [HideInInspector]
    public Vector3 hitDir = Vector3.zero;
    [HideInInspector]
    public GameObject mainBulletPoint;
    

    private void Update()
    {
        hasHitTarget = GetHitPosition(out hitDir, out hitTarget);
        Vector3 dir = hitDir;
        if (hasHitTarget)
        {
            if (mainBulletPoint != null)
            {
                Vector3 bulletPivot = transform.InverseTransformPoint(mainBulletPoint.transform.position);
                bulletPivot.y = 0;
                bulletPivot = transform.TransformPoint(bulletPivot);
                dir = (hitTarget.point - bulletPivot).normalized;
#if UNITY_EDITOR
                
                DebugExtension.DebugPoint(bulletPivot, Color.blue);
                DebugExtension.DebugPoint(hitTarget.point, Color.red);
#endif
            }
        }
        Vector3 relativeDir = m_WholeBody.transform.InverseTransformDirection(dir);
        //Debugger.Log(relativeDir);
        Quaternion rotation = Quaternion.FromToRotation(Vector3.down, relativeDir);
        //Debugger.Log(rotation.eulerAngles);
        transform.localRotation = rotation;

        //修正手轴转向
        Vector3 v1 = Vector3.ProjectOnPlane(m_WholeBody.transform.up, dir);
        Vector3 v2 = Vector3.ProjectOnPlane(transform.forward, dir);
#if UNITY_EDITOR
        DebugExtension.DebugArrow(transform.position, dir, Color.yellow);
        DebugExtension.DebugArrow(transform.position, v1, Color.yellow);
        DebugExtension.DebugArrow(transform.position, v2, Color.yellow);
#endif
        float angleZ = Vector3.SignedAngle(v1, v2, dir);
        transform.Rotate(dir, -angleZ, Space.World);

    }

    bool GetHitPosition(out Vector3 dir, out RaycastHit hit)
    {
        Camera mainCamera = m_Camera;
        hit = new RaycastHit();
        dir = mainCamera.transform.forward;
        Vector3 pos = mainCamera.transform.position;
        Vector3 fixedV = Vector3.ProjectOnPlane(pos - transform.position, mainCamera.transform.up);
        fixedV = Vector3.Project(fixedV, dir);
        pos -= fixedV;
#if UNITY_EDITOR
        Debug.DrawLine(pos, pos+ mainCamera.transform.forward);
#endif
        int layerMask = (~LayerMask.GetMask("Bullet")) & (~LayerMask.GetMask("PlayerMyself")) & (~LayerMask.GetMask("PlayerCharacter"));
        if (Physics.Raycast(pos, mainCamera.transform.forward, out hit, 1000, layerMask))
        {
            dir = (hit.point - transform.position).normalized;
            return true;
        }
        return false;
    }

    public override void OnLookChanged(float deltaAngleX, float deltaAngleY, float allAngleXChanged, float allAngleYChanged)
    {
        //transform.localEulerAngles = new Vector3(-90-allAngleYChanged, 0, 0);
    }

    public override void OnMoveChanged(Vector3 movement)
    {

    }
}