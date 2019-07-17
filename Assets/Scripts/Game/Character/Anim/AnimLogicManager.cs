using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public delegate void LogicCallback(CallbackEvent evt = null);
public delegate void LogicBreakCallback(CallbackBreakEvent evt = null);
public delegate bool CheckLogicCallback(CallbackEvent evt = null);


public enum ELogicState
{
    None = -1,
    TurnOff = 0,
    Run = 1,
    Reload = 2,
    Aim = 3,
    Attack = 4,
    Hit = 5,
    Skill = 6,
    Die = 7,
    Jump = 8,
    TakeGun = 9,
    SwitchWeapon = 10,
    Idle = 11,
    Max = 12,
}

public class CallbackEvent
{
    public CallbackEvent()
    {

    }

    public CallbackEvent(float t)
    {
        time = t;
    }

    public float Time
    {
        set { time = value; }
        get { return time; }
    }

    float time = 10.0f;
}

public class CallbackBreakEvent
{
    CallbackBreakEvent()
    {

    }

    public CallbackBreakEvent(ELogicState st, CallbackEvent evt)
    {
        state = st;
        func_evt = evt;
    }

    public ELogicState state = ELogicState.None;

    public CallbackEvent Event
    {
        set { func_evt = value; }
        get { return func_evt; }
    }

    CallbackEvent func_evt = null;
}

public class CallbackLogic
{
    public const float INF_TIME = 9999;

    bool func_finish = false;
    LogicCallback func_callback = null;
    LogicBreakCallback func_break = null;
    CheckLogicCallback func_check = null;
    CallbackEvent func_evt = null;

    public bool IsExecute
    {
        set { }
        get { return func_finish; }
    }

    public CallbackEvent Event
    {
        set { }
        get { return func_evt; }
    }

    CallbackLogic()
    {
        func_callback = null;
        func_check = ReachTime;
        func_evt = new CallbackEvent(INF_TIME);
        func_break = null;
    }

    public CallbackLogic(float time, LogicCallback endFunc = null, LogicBreakCallback breakFunc = null)
    {
        func_callback = endFunc;
        func_check = ReachTime;
        func_evt = new CallbackEvent(time);
        func_break = breakFunc;
    }

    public CallbackLogic(LogicCallback func, CallbackEvent evt = null, LogicBreakCallback bfunc = null)
    {
        func_callback = func;
        func_check = ReachTime;
        func_evt = evt;
        func_break = bfunc;
    }

    public CallbackLogic(LogicCallback func, CheckLogicCallback check = null, CallbackEvent evt = null, LogicBreakCallback bfunc = null)
    {
        func_callback = func;
        func_check = check;
        func_evt = evt;
        func_break = bfunc;
    }


    bool ReachTime(CallbackEvent evt = null)
    {
        if (evt != null)
        {
            evt.Time -= Time.deltaTime;
            if (evt.Time <= 0)
                return true;
        }

        return false;
    }

    public bool Check()
    {
        if (func_check != null)
        {
            return func_check(func_evt);
        }

        return false;
    }

    public void Callback()
    {
        if (func_callback != null)
        {
            func_callback(func_evt);
            func_finish = true;
        }
    }

    public void BreakCallback(CallbackBreakEvent evt)
    {
        if (func_break != null && evt != null)
        {
            func_break(evt);
            func_finish = true;
        }
    }
}

public class LogicState
{
    // 0 忽略
    // 1 打断
    // 2 追加

    // different soldier can have different relations
    // state begins with zero
    public int[,] relations = new int[(int)ELogicState.Max, (int)ELogicState.Max]
    {
    
     // row new state, col old state 
                        //Turnoff Run Reload  Aim  Shoot   Hit Skill Die Jump TakeGun SwichWeapon Idle 
/*TurnOff*/             {  0,     0,   2,     2,     2,    2,   2,   0,   0,   2,      2,           1},
/*Run*/                 {  1,     1,   2,     2,     2,    2,   2,   0,   1,   2,      2,           1},
/*Reload*/              {  2,     2,   0,     2,     0,    1,   0,   0,   2,   2,      0,           2},
/*Aim*/                 {  1,     2,   0,     0,     0,    1,   0,   0,   2,   1,      0,           2},
/*Shoot*/               {  0,     2,   0,     1,     1,    1,   0,   0,   2,   1,      0,           2},
/*Hit*/                 {  2,     2,   2,     0,     0,    0,   0,   0,   2,   1,      1,           2},
/*Skill*/               {  2,     2,   1,     1,     1,    1,   0,   0,   2,   1,      2,           2},
/*Die*/                 {  1,     1,   1,     1,     1,    1,   1,   0,   1,   1,      1,           1},
/*Jump*/                {  1,     1,   2,     2,     2,    2,   2,   0,   0,   2,      2,           1},
/*TakeGun*/             {  2,     2,   2,     1,     1,    1,   0,   0,   2,   0,      2,           2},
/*SwichWeapon*/         {  2,     2,   1,     2,     1,    1,   2,   0,   2,   2,      0 ,          2},
/*Idle*/                {  0,     1,   2,     2,     2,    2,   2,   0,   1,   2,      2,           0},

    };


    int logic_state = 0;
    int state_length = (int)ELogicState.Max;

    /// <summary>
    /// value must be a two-dimensional array
    /// row must be equal to col 
    /// row and col should be no more than 32
    /// </summary>
    /// <param name="value"></param>
    public bool SetRelations(int[,] value)
    {
        if (value != null)
        {
            if (value.GetLength(0) <= 0 || value.GetLength(0) > 32 || value.GetLength(0) != value.GetLength(1))
            {
                return false;
            }

            state_length = value.GetLength(0);
            relations = value;
        }
        else
        {
            return false;
        }

        return true;
    }


    public int State
    {
        set { }
        get { return logic_state; }
    }


    /// <summary>
    /// 查询状态是否可以添加
    /// </summary>
    /// <param name="inState"></param>
    /// <returns></returns>
    public bool CheckAddState(ELogicState inState)
    {
        int state = (int)inState;
        if (state >= state_length)
            return false;

        for (int i = 0; i < state_length; i++)
        {
            if (CheckState((ELogicState)i))
            {
                int relation = relations[state, i];

                if (relation == 0)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 添加状态
    /// </summary>
    /// <param name="inState"></param>
    /// <returns></returns>
    public bool AddState(ELogicState inState)
    {
        int state = (int)inState;
        if (state >= state_length)
            return false;

        logic_state |= (1 << state);

        return true;
    }

    public bool CheckState(ELogicState state)
    {
        int conv = 1 << (int)state;

        if ((logic_state & conv) != 0)
        {
            return true;
        }

        return false;
    }

    public bool ClearState(ELogicState state)
    {
        if (CheckState(state))
        {
            // 取反更好
            logic_state -= (1 << (int)state);
            return true;
        }

        return false;
    }

    public void ClearAllState()
    {
        logic_state = 0;
    }

}

public class AnimLogicManager
{
    bool _locked = false;
    string _lockReason = "";
    float _locktime = 0;
    bool _enable = true;

    public bool Enable
    {
        get { return _enable; }
        set { _enable = value; }
    }

    protected LogicState logic_state = new LogicState();
    protected Dictionary<int, CallbackLogic> callbacks = new Dictionary<int, CallbackLogic>();

    ActorPlayer _owner = null;

    public AnimLogicManager(ActorPlayer inOwner)
    {
        _owner = inOwner;
    }

    public int State
    {
        get { return logic_state.State; }
    }

    public int[,] Relations
    {
        get { return logic_state.relations; }
    }

    public bool CheckState(ELogicState state)
    {
        return logic_state.CheckState(state);
    }

    public void Lock(float time, string reason = "")
    {
        //只有自己有操作僵直
        if (!InGameManager.Instance.IsMe(_owner))
            return;

        if (!_locked && time > 0)
        {
            _locked = true;
            _locktime = time;
            _lockReason = reason;
        }
    }

    public void Unlock()
    {
        if (_locked)
        {
            _locked = false;
            _locktime = 0;
            _lockReason = "";
        }
    }

    public bool IsLock()
    {
        return _locked;
    }

    public virtual void ClearAllState()
    {
        logic_state.ClearAllState();
        callbacks.Clear();
    }

    public bool ClearState(ELogicState inState)
    {
        return logic_state.ClearState(inState);

    }

    public bool CheckAddState(ELogicState inState)
    {
        return logic_state.CheckAddState(inState);
    }

    /// <summary>
    /// 添加一个逻辑状态
    /// </summary>
    /// <param name="state">State分类</param>
    /// <param name="callback">回调函数</param>
    /// <param name="check">检查是否可以AddState</param>
    /// <returns></returns>
    /// 

    public bool Add(ELogicState state, CallbackLogic callback = null, bool check = true)
    {
        if (!_enable)
            return true;

        if (check)
        {
            if (_locked)
                return false;

            if (!logic_state.CheckAddState(state))
            {
                return false;
            }
        }


        // 打断之前状态
        for (int i = 0; i < (int)ELogicState.Max; i++)
        {
            ELogicState lastState = (ELogicState)i;

            if (CheckState(lastState))
            {
                int relation = logic_state.relations[(int)state, i];

                //打断
                if (relation == 1)
                {
                    logic_state.ClearState(lastState);

                    // 打断回调
                    if (callbacks.ContainsKey((int)lastState) && callbacks[(int)lastState] != null)
                    {
                        callbacks[(int)lastState].BreakCallback(new CallbackBreakEvent(state, callbacks[(int)lastState].Event));

                        callbacks.Remove((int)lastState);
                    }
                }
            }
        }

        // 添加状态
        logic_state.AddState(state);

        if (callback == null)
        {
            callback = new CallbackLogic(CallbackLogic.INF_TIME);
        }

        // 添加打断
        if (callbacks.ContainsKey((int)state))
        {
            // 设置新状态
            callbacks[(int)state] = callback;
        }
        else
        {
            callbacks.Add((int)state, callback);
        }

        PrintState();
        return true;
    }

    public void Update()
    {
        if (!_enable)
            return;

        if (_locked)
        {
            _locktime -= Time.deltaTime;

            if (_locktime <= 0)
                _locked = false;
        }

        if (callbacks.Count == 0)
            return;

        for (int i = 0; i < (int)ELogicState.Max; i++)
        {
            if (CheckState((ELogicState)i))
            {
                var state = (ELogicState)i;
                if (callbacks.ContainsKey((int)state))
                {
                    var call = callbacks[(int)state];

                    //完成
                    if (call.Check())
                    {
                        // 清理逻辑状态
                        logic_state.ClearState(state);

                        // 条件达成执行回调
                        call.Callback();

                        // 删除完成的回调
                        callbacks.Remove((int)state);
                    }
                }
            }
        }
    }

    /// TEST /////////////////////////////////////////////////////////////////////////////
    int lastState;
    void PrintState()
    {

        //if (!BattleHelper.IsMe(_owner))
        //    return;

        if (logic_state.State == lastState)
            return;

        lastState = logic_state.State;

        string str = _owner.name +  "STATE:[ ";

        for (int i = 0; i < (int)ELogicState.Max; i++)
        {
            if (CheckState((ELogicState)i))
            {
                if (i == 0)
                    str += "turn off";
                else if (i == 1)
                    str += "run ";
                else if (i == 2)
                    str += "reload ";
                else if (i == 3)
                    str += "aim ";
                else if (i == 4)
                    str += "attack ";
                else if (i == 5)
                    str += "hit ";
                else if (i == 6)
                    str += "skill ";
                else if (i == 7)
                    str += "die ";
                else if (i == 8)
                    str += "jump ";
                else if (i == 9)
                    str += "takegun ";
                else if (i == 10)
                    str += "switchweapon ";
                else if (i == 11)
                    str += "idle";
            }
        }

        Debugger.Log(str + "]");
    }

}