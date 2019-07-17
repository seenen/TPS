using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PureMVC.Interfaces;
using PureMVC.Patterns.Mediator;

public struct WeaponClipInfo
{
    public string playerId;
    public int curClip;
    public int clipMax;
}

public struct BulletShotInfo
{
    public BaseBullet bullet;
}

public class TestWeaponHudMediator : Mediator
{
    public TestWeaponHudMediator(string mediatorName, object viewComponent = null) : base(mediatorName, viewComponent)
    {
    }
    public GlobalProxy globalProxy;
    public string playerId = "";

    public override void OnRegister()
    {
        base.OnRegister();
        globalProxy = (GlobalProxy)NGlobal.Facade.RetrieveProxy(GlobalProxy.NAME);
    }

    public override void HandleNotification(INotification notification)
    {
        string name = notification.Name;
        object body = notification.Body;
        if (name == Const.Notify.UpdateWeaponClip)
        {
            WeaponClipInfo clipInfo = (WeaponClipInfo)body;
            if (clipInfo.playerId == playerId)
            {
                ((TestWeaponHud)ViewComponent).UpdateWeaponClip(clipInfo.curClip, clipInfo.clipMax);
            }
        }
        else if (name == Const.Notify.UpdateBulletShot)
        {
            BulletShotInfo bulletInfo = (BulletShotInfo)body;
            if (bulletInfo.bullet.playerId == playerId)
            {
                ((TestWeaponHud)ViewComponent).UpdateBulletShot(bulletInfo.bullet);
            }
        }
    }

    public override string[] ListNotificationInterests()
    {
        return new string[] {
            Const.Notify.UpdateWeaponClip,
            Const.Notify.UpdateBulletShot,
        };
    }
}
