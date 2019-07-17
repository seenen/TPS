using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ObjectPoolItemState
{
    Valid = 0,
    Used = 1,
}

public class ObjectPoolData
{
    public string name;
    public string path;
    public int min;
    public int max;
    public GameObject prefab;
    public ObjectPoolItemState state;
    public List<ObjectPoolItem> items = new List<ObjectPoolItem>();
}

public class ObjectPoolItem
{
    public string name;
    public GameObject gameObject;
    public ObjectPoolItemState state;
    public float instantiateTime;
}

public class ObjectPoolManager : BaseManager
{
    static ObjectPoolManager m_Instance;
    public new static string NAME = "ObjectPoolManager";
    public new static ObjectPoolManager Instance
    {
        get
        {
            if (m_Instance == null) m_Instance = GetInstance<ObjectPoolManager>(NAME);
            return m_Instance;
        }
    }



    GameObject poolObject = null;
    static Dictionary<string, ObjectPoolData> objectPoolDataMap = new Dictionary<string, ObjectPoolData>();

    public override void OnRegister()
    {
        base.OnRegister();
        Init();
    }

    public override void OnRemove()
    {
        base.OnRemove();
        ReleaseObjectPool();
    }


    public void Init()
    {
        poolObject = GameObject.Find("ObjectPool");
        if (poolObject == null)
        {
            poolObject = new GameObject("ObjectPool");
            poolObject.transform.position = Vector3.zero;
            poolObject.transform.localScale = Vector3.one;
        }

    }
    
    public void Register(string name,string path,int min,int max)
    {
        ObjectPoolData opd = GetObjectPoolData(name);
        opd.name = name;
        opd.path = path;
        opd.min = min;
        opd.max = max;
    }

    public void Register(string name, GameObject prefab, int min, int max)
    {
        ObjectPoolData opd = GetObjectPoolData(name);
        opd.name = name;
        opd.prefab = prefab;
        opd.min = min;
        opd.max = max;
        
    }

    public GameObject Instantiate(string name)
    {
        ObjectPoolData opd = GetObjectPoolData(name);
        if (opd.prefab != null)
        {
            ObjectPoolItem item = null;
            for (int i = 0; i < opd.items.Count; i++)
            {
                if(opd.items[i].state == ObjectPoolItemState.Valid)
                {
                    item = opd.items[i];
                    break;
                }
            }
            if (item == null) {
                item = new ObjectPoolItem();
                item.name = name;
                item.gameObject = GameObject.Instantiate(opd.prefab);
                item.gameObject.name = name;
                opd.items.Add(item);
            }
            item.state = ObjectPoolItemState.Used;
            item.gameObject.transform.localPosition = Vector3.zero;
            item.gameObject.transform.localScale = opd.prefab.transform.localScale;
            item.gameObject.transform.localRotation = opd.prefab.transform.localRotation;
            item.instantiateTime = Time.time;
            return item.gameObject;
        }
        return null;
    }

    public void Release(string name,GameObject obj)
    {
        ObjectPoolData opd = GetObjectPoolData(name);
        if (opd.prefab != null)
        {
            for (int i = 0; i < opd.items.Count; i++)
            {
                if (opd.items[i].gameObject == obj)
                {
                    opd.items[i].state = ObjectPoolItemState.Valid;
                    obj.SetActive(false);
                    obj.transform.SetParent(poolObject.transform);
                    break;
                }
            }
        }
    }

    ObjectPoolData GetObjectPoolData(string name)
    {
        if (!objectPoolDataMap.ContainsKey(name))
        {
            ObjectPoolData opd = new ObjectPoolData();
            objectPoolDataMap.Add(name, opd);
        }
        return objectPoolDataMap[name];
    }

    public void ReleaseObjectPool()
    {

    }
}
