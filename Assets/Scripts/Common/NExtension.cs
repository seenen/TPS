using Net.ProtolJava;
using UnityEngine;

public static class NExtension
{
    public static bool NEquals(this float a, float b)
    {
        return Mathf.Abs(a - b) <= NConstValue.Epsilon;
    }
    public static bool NUnEquals(this float a, float b)
    {
        return !a.NEquals(b);
    }
    public static bool NEquals(this Vector2 a, Vector2 b)
    {
        return a.x.NEquals(b.x) && a.y.NEquals(b.y);
    }

    public static bool NUnEquals(this Vector2 a, Vector2 b)
    {
        return !a.x.NEquals(b.x) || !a.y.NEquals(b.y);
    }

    public static bool NEquals(this Vector3 a, Vector3 b)
    {
        return a.x.NEquals(b.x) && a.y.NEquals(b.y) && a.z.NEquals(b.z);
    }

    public static bool NUnEquals(this Vector3 a, Vector3 b)
    {
        return !a.x.NEquals(b.x) || !a.y.NEquals(b.y) || !a.z.NEquals(b.z);
    }

    public static bool NEquals(this PVector3 a, Vector3 b)
    {
        return a.X.NEquals(b.x) && a.Y.NEquals(b.y) && a.Z.NEquals(b.z);
    }

    public static bool NUnEquals(this PVector3 a, Vector3 b)
    {
        return !a.X.NEquals(b.x) || !a.Y.NEquals(b.y) || !a.Z.NEquals(b.z);
    }

    public static bool NEquals(this Vector3 a, PVector3 b)
    {
        return a.x.NEquals(b.X) && a.y.NEquals(b.Y) && a.z.NEquals(b.Z);
    }

    public static bool NUnEquals(this Vector3 a, PVector3 b)
    {
        return !a.x.NEquals(b.X) || !a.y.NEquals(b.Y) || !a.z.NEquals(b.Z);
    }

    public static PVector3 ToPV3(this Vector3 b)
    {
        PVector3 a = new PVector3();
        a.X = b.x;
        a.Y = b.y;
        a.Z = b.z;
        return a;
    }

    public static Vector3 ToV3(this PVector3 a)
    {
        return new Vector3(a.X,a.Y,a.Z);
    }

    public static Vector2 ToV2(this PVector3 b)
    {
        Vector2 a = new Vector2();
        a.x = b.X;
        a.y = b.Z;
        return a;
    }

    public static PVector3 ToPV3(this Vector2 b)
    {
        PVector3 a = new PVector3();
        a.X = b.x;
        a.Y = 0;
        a.Z = b.y;
        return a;
    }

    public static float AngleBetween(Vector2 v1, Vector2 v2)
    {
        float sin = v1.x * v2.y - v2.x * v1.y;
        float cos = v1.x * v2.x + v1.y * v2.y;

        return Mathf.Atan2(sin, cos) * (180 / Mathf.PI);
    }

    public static Vector2 GetDirByAngle(float angle)
    {
        if(Mathf.Abs(angle) <= 1f)
        {
            return new Vector2(0, 1);
        }
        else if (Mathf.Abs(angle - 90f) <= 1f)
        {
            return new Vector2(1, 0);
        }
        else if (Mathf.Abs(angle - 180f) <= 1f)
        {
            return new Vector2(0, -1);
        }
        else if (Mathf.Abs(angle + 90f) <= 1f)
        {
            return new Vector2(-1, 0);
        }
        return new Vector2(0, 0);
    }
}
