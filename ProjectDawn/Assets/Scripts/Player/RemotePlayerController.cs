using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Remote player controller with buffered snapshot interpolation.
/// </summary>
public class RemotePlayerController : MonoBehaviour
{
    private class Snapshot
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp;
    }

    [Header("Interpolation Settings")]
    [SerializeField] private float bufferTime = 0.1f;       // render ~100ms in the past
    [SerializeField] private float maxExtrapolation = 0.2f; // if no data, guess ahead max 200ms

    private readonly List<Snapshot> snapshots = new List<Snapshot>();

    public void Initialize(float moveSpeed, float rotateSpeed) { } // speeds unused here

    void Awake()
    {
        snapshots.Clear();
        snapshots.Add(new Snapshot
        {
            position = transform.position,
            rotation = transform.rotation,
            timestamp = Time.time
        });
    }

    /// <summary>
    /// Called when the server tells us this player moved.
    /// </summary>
    public void SetTargetTransformation(TransformationDC newTransformation)
    {
        snapshots.Add(new Snapshot
        {
            position = new Vector3(
                newTransformation.positionX,
                newTransformation.positionY,
                newTransformation.positionZ
            ),
            rotation = Quaternion.Euler(
                newTransformation.rotationX,
                newTransformation.rotationY,
                newTransformation.rotationZ
            ),
            timestamp = Time.time
        });

        // Avoid unbounded growth
        if (snapshots.Count > 30)
            snapshots.RemoveAt(0);
    }

    void Update()
    {
        if (snapshots.Count < 2)
            return;

        float renderTime = Time.time - bufferTime;

        Snapshot prev = null, next = null;
        for (int i = 0; i < snapshots.Count; i++)
        {
            if (snapshots[i].timestamp <= renderTime)
                prev = snapshots[i];
            if (snapshots[i].timestamp > renderTime)
            {
                next = snapshots[i];
                break;
            }
        }

        if (prev != null && next != null)
        {
            // Normal interpolation
            float t = Mathf.InverseLerp(prev.timestamp, next.timestamp, renderTime);
            transform.position = Vector3.Lerp(prev.position, next.position, t);
            transform.rotation = Quaternion.Slerp(prev.rotation, next.rotation, t);
        }
        else if (prev != null && next == null)
        {
            // Extrapolation if no newer data yet
            float extrapTime = Mathf.Min(Time.time - prev.timestamp, maxExtrapolation);
            transform.position = Vector3.Lerp(transform.position, prev.position, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Slerp(transform.rotation, prev.rotation, Time.deltaTime * 10f);
        }
    }
}
