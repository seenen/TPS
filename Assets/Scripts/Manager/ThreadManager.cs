using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Linq;

//线程管理
public class ThreadManager : BaseManager
{
    static ThreadManager m_Instance;
    public new static string NAME = "ThreadManager";
    public new static ThreadManager Instance
    {
        get
        {
            if (m_Instance == null) m_Instance = GetInstance<ThreadManager>(NAME);
            return m_Instance;
        }
    }

    public struct DelayedQueueItem
    {
        public double time;
        public Action action;
    }

    class Worker
    {
        private Action action;
        public Worker(Action a)
        {
            action = a;
        }
        public void Run()
        {
            action();
        }
    }

    static int numThreads;
    static List<Thread> m_TreadList = new List<Thread>();
    static List<Action> _actions = new List<Action>();
    static List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();
    static List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();
    static List<Action> _currentActions = new List<Action>();
    private bool hasReleased = false;


    public override void OnRegister()
    {

    }

    public override void OnRemove()
    {
        //自动销毁所有工作中的线程
        lock (m_TreadList)
        {
            for (int i = m_TreadList.Count - 1; i >= 0; i--)
            {
                try
                {
                    m_TreadList[i].Abort();
                }
                catch (Exception e)
                {
                    Debugger.LogException(e);
                }
            }
            m_TreadList.Clear();
        }
        hasReleased = true;
    }

    public void RunOnMainThread(Action action)
    {
        if (hasReleased) return;
        RunOnMainThread(action, 0);
    }

    public void RunOnMainThread(Action action, float time)
    {
        if (time != 0)
        {
            lock (_delayed)
            {

                _delayed.Add(new DelayedQueueItem { time = DateTime.Now.Ticks / 10000000.0 + time, action = action });
            }
        }
        else
        {
            lock (_actions)
            {
                _actions.Add(action);
            }
        }
    }

    public Thread RunAsync(Action a)
    {
        lock (m_TreadList)
        {
            for (int i = m_TreadList.Count - 1; i >= 0; i--)
            {
                if (!m_TreadList[i].IsAlive)
                {
                    m_TreadList.RemoveAt(i);
                }
            }
        }
        Worker worker = new Worker(a);
        Thread workerThread = new Thread(worker.Run);
        m_TreadList.Add(workerThread);
        workerThread.Start();
        return workerThread;
    }

    // Update is called once per frame
    void Update()
    {
        lock (_actions)
        {
            _currentActions.Clear();
            _currentActions.AddRange(_actions);
            _actions.Clear();
        }
        foreach (var a in _currentActions)
        {
            a();
        }
        lock (_delayed)
        {
            _currentDelayed.Clear();
            _currentDelayed.AddRange(_delayed.Where(d => d.time <= DateTime.Now.Ticks / 10000000.0));
            foreach (var item in _currentDelayed)
                _delayed.Remove(item);
        }
        foreach (var delayed in _currentDelayed)
        {
            delayed.action();
        }
    }
}