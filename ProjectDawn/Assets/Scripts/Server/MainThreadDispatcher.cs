using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Ensures Unity actions run on the main thread.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    void Awake()
    {
        Application.runInBackground = true;

        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue()?.Invoke();
            }
        }
    }

    public static void Enqueue(Action action)
    {
        if (_instance == null)
        {
            GameObject dispatcherObj = new GameObject("MainThreadDispatcher");
            _instance = dispatcherObj.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(dispatcherObj);
        }
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}