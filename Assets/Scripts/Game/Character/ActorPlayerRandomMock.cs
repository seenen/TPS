using UnityEngine;
using System.Collections;

public class ActorPlayerRandomMock : MonoBehaviour
{
    public ActorPlayer[] m_Players;
    System.Random random;
    // Use this for initialization
    void Start()
    {
        random = new System.Random((int)(Time.time * 1000));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (m_Players == null) return;
        for(int i = 0; i < m_Players.Length; i++)
        {
            if (random.NextDouble() < 0.05)
            {
                //MockMove(m_Players[i]);
                MockJump(m_Players[i]);
            }
            if (random.NextDouble() < 0.05)
            {
                MockLook(m_Players[i]);
            }
            /*if (random.NextDouble() < 0.005)
            {
                MockChangeWeapon(m_Players[i]);
            }
            else
            {
                MockChangeWeaponNone(m_Players[i]);
            }*/
            if (random.NextDouble() < 0.08)
            {
                MockShoot(m_Players[i]);
            }
            //m_Players[i].UpdatePlayer(false);
        }
    }

    void MockMove(ActorPlayer player)
    {
        float x = (float)(random.NextDouble() - 0.5) * 2;
        float y = (float)(random.NextDouble() - 0.5) * 2;
        player.SetPlayerHorizontalMove(x);
        player.SetPlayerVerticalMove(y);
    }

    void MockLook(ActorPlayer player)
    {
        float x = (float)(random.NextDouble() - 0.5) * 0.5f;
        float y = (float)(random.NextDouble() - 0.5) * 0.1f;
        player.SetPlayerHorizontalLookMove(x);
        player.SetPlayerVerticalLookMove(y);
    }

    void MockJump(ActorPlayer player)
    {
        player.SetPlayerJump(random.NextDouble() < 0.5);
    }

    void MockChangeWeapon(ActorPlayer player)
    {
        int id = random.Next(0, 3);
        player.SetPlayerChangeWeapon1(id==0);
        player.SetPlayerChangeWeapon2(id==1);
        player.SetPlayerChangeWeapon3(id==2);
    }

    void MockChangeWeaponNone(ActorPlayer player)
    {
        player.SetPlayerChangeWeapon1(false);
        player.SetPlayerChangeWeapon2(false);
        player.SetPlayerChangeWeapon3(false);
    }

    void MockShoot(ActorPlayer player)
    {
        StartCoroutine(_MockShoot(player));
    }

    IEnumerator _MockShoot(ActorPlayer player)
    {
        yield return new WaitForFixedUpdate();
        player.SetPlayerShootStart(true);
        player.SetPlayerShooting(true);
        player.SetPlayerShootEnd(false);
        yield return new WaitForFixedUpdate();
        player.SetPlayerShootStart(false);
        player.SetPlayerShooting(false);
        player.SetPlayerShootEnd(true);
        yield return new WaitForFixedUpdate();
        player.SetPlayerShootStart(false);
        player.SetPlayerShootEnd(false);
    }
}
