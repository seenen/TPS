using UnityEngine;
using System;
using PureMVC.Interfaces;

public class BaseManager : MonoBehaviour , INotifier, IMediator
{
    public static string NAME = "BaseManager";
    static BaseManager m_Instance;
    public static BaseManager Instance
    {
        get
        {
            if (m_Instance == null) m_Instance = GetInstance<BaseManager>(NAME);
            return m_Instance;
        }
    }

    protected static T GetInstance<T>(string name) where T: BaseManager
    {
        GameObject go = new GameObject();
        DontDestroyOnLoad(go);
        T instance = go.AddComponent<T>();
        go.name = name;
        instance.MediatorName = name;
        return (T)instance;
    }

    /// <summary>
    /// List the <c>INotification</c> names this
    /// <c>Mediator</c> is interested in being notified of.
    /// </summary>
    /// <returns>the list of <c>INotification</c> names</returns>
    public virtual string[] ListNotificationInterests()
    {
        return new string[0];
    }

    /// <summary>
    /// Handle <c>INotification</c>s.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Typically this will be handled in a switch statement,
    ///         with one 'case' entry per <c>INotification</c>
    ///         the <c>Mediator</c> is interested in.
    ///     </para>
    /// </remarks>
    /// <param name="notification"></param>
    public virtual void HandleNotification(INotification notification)
    {
    }

    /// <summary>
    /// Called by the View when the Mediator is registered
    /// </summary>
    public virtual void OnRegister()
    {
    }

    /// <summary>
    /// Called by the View when the Mediator is removed
    /// </summary>
    public virtual void OnRemove()
    {
    }

    /// <summary>the mediator name</summary>
    public string MediatorName { get ; protected set; }

    /// <summary>The view component</summary>
    public object ViewComponent { get; set; }


    /// <summary>
    /// Create and send an <c>INotification</c>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Keeps us from having to construct new INotification 
    ///         instances in our implementation code.
    ///     </para>
    /// </remarks>
    /// <param name="notificationName">the name of the notiification to send</param>
    /// <param name="body">the body of the notification (optional)</param>
    /// <param name="type">the type of the notification (optional)</param>
    public virtual void SendNotification(string notificationName, object body = null, string type = null)
    {
        Facade.SendNotification(notificationName, body, type);
    }

    /// <summary>
    /// Initialize this INotifier instance.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is how a Notifier gets its multitonKey. 
    ///         Calls to sendNotification or to access the
    ///         facade will fail until after this method 
    ///         has been called.
    ///     </para>
    ///     <para>
    ///         Mediators, Commands or Proxies may override 
    ///         this method in order to send notifications
    ///         or access the Multiton Facade instance as
    ///         soon as possible. They CANNOT access the facade
    ///         in their constructors, since this method will not
    ///         yet have been called.
    ///     </para>
    /// </remarks>
    /// <param name="key">the multitonKey for this INotifier to use</param>
    public void InitializeNotifier(string key)
    {
        MultitonKey = key;
    }

    /// <summary> Return the Multiton Facade instance</summary>
    protected IFacade Facade
    {
        get
        {
            if (MultitonKey == null) throw new Exception(MULTITON_MSG);
            return PureMVC.Patterns.Facade.Facade.GetInstance(MultitonKey, () => new PureMVC.Patterns.Facade.Facade(MultitonKey));
        }
    }

    /// <summary>The Multiton Key for this app</summary>
    public string MultitonKey { get; protected set; }

    /// <summary>Message Constants</summary>
    protected string MULTITON_MSG = "multitonKey for this Notifier not yet initialized!";
}