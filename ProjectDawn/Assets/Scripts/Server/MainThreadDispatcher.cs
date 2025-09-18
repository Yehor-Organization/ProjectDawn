using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ensures that code coming from background threads (e.g. SignalR handlers)
/// can be executed safely on Unity's main thread.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    /// <summary>
    /// Enqueue an action to run on the main Unity thread during the next Update().
    /// </summary>
    public static void Enqueue(Action action)
    {
        if (action == null) return;

        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }

        // Ensure instance exists
        if (_instance == null)
        {
            var obj = new GameObject("MainThreadDispatcher");
            _instance = obj.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(obj);
        }
    }

    private void Awake()
    {
        Application.runInBackground = true;

        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                try
                {
                    var action = _executionQueue.Dequeue();
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainThreadDispatcher] Exception in queued action: {ex}");
                }
            }
        }
    }
}
