using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using PureMVC.Patterns.Mediator;

public class TestWeaponHud : ViewComponent
{
    public Text clipText;
    public Text headShotText;
    public Text bodyShotText;

    int curClip = 0;
    int maxClip = 0;

    int headShot = 0;
    int bodyShot = 0;

    TestWeaponHudMediator weaponHudMediator;

    // Use this for initialization
    void Awake()
    {
        weaponHudMediator = new TestWeaponHudMediator("WeaponHudMediator",this);
    }

    private void OnEnable()
    {
        NGlobal.Facade.RegisterMediator(weaponHudMediator);
        weaponHudMediator.playerId = weaponHudMediator.globalProxy.GetUUid();
        UpdateWeaponClip();
        UpdateBulletShotInfo();
    }

    private void OnDisable()
    {
        NGlobal.Facade.RemoveMediator(weaponHudMediator.MediatorName);
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public void UpdateWeaponClip(int cur,int max)
    {
        curClip = cur;
        maxClip = max;
        UpdateWeaponClip();
    }

    public void UpdateWeaponClip()
    {
        clipText.text = curClip + "/" + maxClip;
    }

    public void UpdateBulletShot(BaseBullet bullet)
    {
        RaycastHit hit;
        if(bullet.GetFinalHited(out hit))
        {
            if(hit.collider.tag == "PlayerHead")
            {
                headShot++;
            }
            else if(hit.collider.tag == "PlayerBody")
            {
                bodyShot++;
            }
        }
        UpdateBulletShotInfo();
    }

    public void UpdateBulletShotInfo()
    {
        headShotText.text = "HeadShot:" + headShot;
        bodyShotText.text = "BodyShot:" + bodyShot;
    }
}
