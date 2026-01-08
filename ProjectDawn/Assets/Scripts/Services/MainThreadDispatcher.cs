using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ensures that code coming from background threads (e.g. SignalR handlers)
/// can be executed safely on Unity's main thread.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static readonly object _lock = new object();
    private static MainThreadDispatcher _instance;

    // Track if we're on main thread
    private static int _mainThreadId = -1;

    /// <summary>
    /// Enqueue an action to run on the main Unity thread during the next Update().
    /// </summary>
    public static void Enqueue(Action action)
    {
        if (action == null)
        {
            Debug.LogWarning("[MainThreadDispatcher] Attempted to enqueue null action");
            return;
        }

        // If already on main thread, execute immediately to avoid deadlocks
        if (_mainThreadId != -1 && System.Threading.Thread.CurrentThread.ManagedThreadId == _mainThreadId)
        {
            try
            {
                action.Invoke();
                return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainThreadDispatcher] Exception in immediate action: {ex}");
                return;
            }
        }

        // Otherwise queue for main thread
        lock (_lock)
        {
            _executionQueue.Enqueue(action);
        }

        // Ensure instance exists
        EnsureInstance();
    }

    // Diagnostic method
    public static int GetQueueCount()
    {
        lock (_lock)
        {
            return _executionQueue.Count;
        }
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
            return;

        // Find existing instance first
        _instance = FindObjectOfType<MainThreadDispatcher>();

        if (_instance == null)
        {
            var obj = new GameObject("MainThreadDispatcher");
            _instance = obj.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(obj);
            Debug.Log("[MainThreadDispatcher] Instance created dynamically");
        }
    }

    private void Awake()
    {
        // Store main thread ID
        _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

        Application.runInBackground = true;

        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[MainThreadDispatcher] Initialized in Awake");
        }
        else if (_instance != this)
        {
            Debug.LogWarning("[MainThreadDispatcher] Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            Debug.Log("[MainThreadDispatcher] Instance destroyed");
            _instance = null;
        }
    }

    private void Update()
    {
        // Process all queued actions
        int processedCount = 0;
        const int maxActionsPerFrame = 1000; // Safety limit

        // Create a temporary list to avoid holding the lock too long
        List<Action> actionsToExecute = null;

        lock (_lock)
        {
            if (_executionQueue.Count > 0)
            {
                int count = Mathf.Min(_executionQueue.Count, maxActionsPerFrame);
                actionsToExecute = new List<Action>(count);

                for (int i = 0; i < count; i++)
                {
                    actionsToExecute.Add(_executionQueue.Dequeue());
                }
            }
        }

        // Execute outside of lock to prevent deadlocks
        if (actionsToExecute != null)
        {
            foreach (var action in actionsToExecute)
            {
                try
                {
                    action?.Invoke();
                    processedCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MainThreadDispatcher] Exception in queued action: {ex}");
                }
            }
        }

        // Warn if queue is backing up
        if (processedCount >= maxActionsPerFrame)
        {
            int remaining;
            lock (_lock)
            {
                remaining = _executionQueue.Count;
            }

            if (remaining > 0)
            {
                Debug.LogWarning($"[MainThreadDispatcher] Processed {processedCount} actions, {remaining} still queued");
            }
        }
    }
}