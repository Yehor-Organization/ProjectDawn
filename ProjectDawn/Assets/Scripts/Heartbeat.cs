using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

public class Heartbeat : MonoBehaviour
{
    private float last;

    private void Start()
    {
        // Find all MonoBehaviours with OnGUI methods
        var allObjects = FindObjectsOfType<MonoBehaviour>();
        var withOnGUI = allObjects
            .Where(mb => mb.GetType().GetMethod("OnGUI",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic) != null)
            .ToList();

        Debug.Log($"🔍 Found {withOnGUI.Count} scripts with OnGUI:");
        foreach (var script in withOnGUI)
        {
            Debug.Log($"   - {script.GetType().Name} on {script.gameObject.name}");
        }
    }

    private void Update()
    {
        if (Time.time - last > 1f)
        {
            Debug.Log($"❤️ Heartbeat | FPS: {1f / Time.deltaTime:F1}");
            last = Time.time;
        }
    }
}