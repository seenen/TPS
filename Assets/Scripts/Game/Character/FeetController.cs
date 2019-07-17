using UnityEngine;
using System.Collections;

public class FeetController : BaseBodyController
{
    public float feetMoveAngle = 30;
    float lastFeetAngle = 0;
    float curFeetAngle = 0;
    public override void OnLookChanged(float deltaAngleX, float deltaAngleY, float allAngleXChanged, float allAngleYChanged)
    {
        if (Mathf.Abs(allAngleXChanged - lastFeetAngle) > feetMoveAngle)
        {
            lastFeetAngle = allAngleXChanged;
        }
        transform.localEulerAngles = new Vector3(0, lastFeetAngle - allAngleXChanged, 0);
        curFeetAngle = allAngleXChanged;
    }

    public override void OnMoveChanged(Vector3 movement)
    {
        lastFeetAngle = curFeetAngle;
        transform.localEulerAngles = new Vector3(0, lastFeetAngle - curFeetAngle, 0);
    }
}
