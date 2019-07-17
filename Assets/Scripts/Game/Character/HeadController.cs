using UnityEngine;
using System.Collections;

public class HeadController : BaseBodyController
{
    public override void OnLookChanged(float deltaAngleX, float deltaAngleY, float allAngleXChanged, float allAngleYChanged)
    {
        transform.localEulerAngles = new Vector3(-allAngleYChanged, 0, 0);
    }

    public override void OnMoveChanged(Vector3 movement)
    {

    }
}