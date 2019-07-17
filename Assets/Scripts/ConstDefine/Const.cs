using System;
using System.Collections.Generic;

public class Const
{
    public static bool OpenDebugInfo = true;
    public static float FrameTime = 0.016f;
    public static int OverServerMaxFrame = 20;
    public static int OverServerMinFrame = 10;
    public static float OverServerFrameFixed = 5;
    public static int FrameStuckLimit = 30;
    public class Notify
    {
        public static string UpdateWeaponClip = "UpdateWeaponClip";
        public static string UpdateBulletShot = "UpdateBulletShot";
    }
}