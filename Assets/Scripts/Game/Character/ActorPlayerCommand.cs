using UnityEngine;
using System.Collections.Generic;
public enum ActorCommandFlags
{
    None = (1 << 0),//什么都没做
    HAS_EXECUTED_SELF = (1 << 1),//本地自身已经执行过
    HAS_EXECUTED_OTHERS = (1 << 2),//其他已经执行过
    VERIFIED = (1 << 3)//服务确认过
}

public class ActorPlayerCommandInput
{
    //当前值
    public Vector3 position;//当前位置
    public Vector3 moveVelocity;//速度
    public float rotationX;//当前水平方向旋转
    public float rotationY;//当前垂直方向旋转
    //变化值
    public float horizontalMove;//水平移动
    public float verticalMove;//垂直移动
    public float horizontalLookMove;//水平视角移动
    public float verticalLookMove;//垂直视角移动
    public bool jump;
    public bool shootStart;
    public bool shooting;
    public bool shootEnd;
    public bool changeWeapon1;
    public bool changeWeapon2;
    public bool changeWeapon3;

    public bool hasHit;
    public Vector3 aimDir;
    public Vector3 hit;
}

public class ActorPlayerCommandResult
{
    public Vector3 position;
    public Vector3 moveVelocity;
    public float horizontalMove;
    public float verticalMove;
    public bool hasChangeWeapon;
    public int changeWeaponId;
    public Vector3 movement;
    public float angleX;
    public float angleY;
    public float rotationX;
    public float rotationY;
    public bool shootStart;
    public bool shooting;
    public bool shootEnd;

    public bool hasHit;
    public Vector3 aimDir;
    public Vector3 hit;
}

public class ActorPlayerCommand
{
    public int sequence;          //指令序号
    public string playerId = "";
    public Dictionary<string,ActorPlayerCommandInput> inputs = new Dictionary<string, ActorPlayerCommandInput>();     //操作指令的输入
    public Dictionary<string, ActorPlayerCommandResult> results = new Dictionary<string, ActorPlayerCommandResult>();   //操作指令执行后得到的结果
    public int flags = (int)ActorCommandFlags.None;
}