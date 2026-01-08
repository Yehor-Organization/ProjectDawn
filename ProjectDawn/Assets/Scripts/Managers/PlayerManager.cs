using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerManager : MonoBehaviour
{
    private readonly Dictionary<int, GameObject> remotePlayers = new();

    private CameraManager cameraManager;

    [Header("Default Settings")]
    [SerializeField] private float defaultMoveSpeed = 15f;

    [SerializeField] private float defaultRotateSpeed = 300f;

    [Header("Spawn Settings")]
    [SerializeField] private Transform defaultSpawnPoint;

    [Header("Prefabs")]
    [SerializeField] private GameObject localPlayerPrefab;

    // Cached (lazy)
    private ObjectManager objectManager;

    [SerializeField] private float positionUpdateThreshold = 0.01f;
    [SerializeField] private GameObject remotePlayerPrefab;
    [SerializeField] private float rotationUpdateThreshold = 0.01f;
    [SerializeField] private float spawnHeightBuffer = 2f;

    private CameraManager CameraManager
    {
        get
        {
            if (cameraManager == null)
                cameraManager = Core.Instance?.Managers?.CameraManager;

            if (cameraManager == null)
                throw new InvalidOperationException(
                    "[PlayerManager] CameraManager not available");

            return cameraManager;
        }
    }

    // -----------------------
    // Lazy dependencies
    // -----------------------
    private ObjectManager ObjectManager
    {
        get
        {
            if (objectManager == null)
                objectManager = Core.Instance?.Managers?.ObjectManager;

            if (objectManager == null)
                throw new InvalidOperationException(
                    "[PlayerManager] ObjectManager not available");

            return objectManager;
        }
    }

    // -----------------------
    // Public API
    // -----------------------
    public void ClearAllPlayers()
    {
        ClearAllRemotePlayers();

        var local = FindObjectOfType<LocalPlayerController>();
        if (local != null)
            Destroy(local.gameObject);
    }

    public void ClearAllRemotePlayers()
    {
        foreach (var kvp in remotePlayers)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        remotePlayers.Clear();
    }

    public int GetLocalPlayerID()
    {
        var auth = Core.Instance?.Services?.AuthService
            ?? throw new InvalidOperationException(
                "[PlayerManager] AuthService not available.");

        var token = auth.GetValidAccessToken()
            .GetAwaiter()
            .GetResult();

        var payload = JWTUtils.Decode(token);

        return payload.PlayerId; // or payload.Sub
    }

    public void RemovePlayer(int playerId)
    {
        if (!remotePlayers.TryGetValue(playerId, out var playerGO))
            return;

        Debug.Log($"[PlayerManager] Removing player {playerId}");
        Destroy(playerGO);
        remotePlayers.Remove(playerId);
    }

    public void SpawnLocalPlayer()
    {
        int myPlayerId = GetLocalPlayerId();

        Debug.Log($"[PlayerManager] Spawning LOCAL player (self) {myPlayerId}");

        SpawnPlayer(myPlayerId, isLocalPlayer: true);
    }

    public void SpawnPlayer(int playerId, bool isLocalPlayer)
    {
        Vector3 spawnPos = defaultSpawnPoint ? defaultSpawnPoint.position : Vector3.zero;
        Quaternion spawnRot = defaultSpawnPoint ? defaultSpawnPoint.rotation : Quaternion.identity;

        if (Terrain.activeTerrain != null)
        {
            float groundY = Terrain.activeTerrain.SampleHeight(spawnPos);
            spawnPos.y = groundY + spawnHeightBuffer;
        }

        if (isLocalPlayer)
        {
            Debug.Log($"[PlayerManager] Spawning LOCAL player {playerId}");

            if (localPlayerPrefab == null)
            {
                Debug.LogError("[PlayerManager] Local player prefab not assigned");
                return;
            }

            var go = Instantiate(localPlayerPrefab, spawnPos, spawnRot);

            var localCtrl = go.GetComponent<LocalPlayerController>();
            if (localCtrl == null)
            {
                Debug.LogWarning("[PlayerManager] LocalPlayerController missing on prefab");
                return;
            }

            localCtrl.Initialize(
                spawnPos,
                defaultMoveSpeed,
                defaultRotateSpeed,
                positionUpdateThreshold,
                rotationUpdateThreshold
            );

            CameraManager.ResetCamera(localCtrl);
        }
        else
        {
            if (remotePlayers.ContainsKey(playerId))
                return;

            Debug.Log($"[PlayerManager] Spawning REMOTE player {playerId}");

            if (remotePlayerPrefab == null)
            {
                Debug.LogError("[PlayerManager] Remote player prefab not assigned");
                return;
            }

            var playerGO = Instantiate(remotePlayerPrefab, spawnPos, spawnRot);
            var remoteCtrl = playerGO.GetComponent<RemotePlayerController>();

            remoteCtrl?.Initialize(defaultMoveSpeed, defaultRotateSpeed);

            remotePlayers.Add(playerId, playerGO);
        }
    }

    public void UpdatePlayerTransformation(int playerId, TransformationDC newTransformation)
    {
        // 🔑 Spawn on first transform
        if (!remotePlayers.TryGetValue(playerId, out var playerObj))
        {
            Debug.Log($"[PlayerManager] Spawning REMOTE player {playerId} from transform");

            SpawnRemotePlayerAtTransform(playerId, newTransformation);
            return;
        }

        var remote = playerObj.GetComponent<RemotePlayerController>();
        if (remote == null)
        {
            Debug.LogWarning($"[PlayerManager] RemotePlayerController missing for {playerId}");
            return;
        }

        remote.SetTargetTransformation(newTransformation);
    }

    private int GetLocalPlayerId()
    {
        var auth = Core.Instance.Services.AuthService;

        var token = auth.GetValidAccessToken().GetAwaiter().GetResult();
        var payload = JWTUtils.Decode(token);

        return payload.PlayerId; // or payload.Sub
    }

    private void SpawnRemotePlayerAtTransform(
        int playerId,
    TransformationDC transform)
    {
        if (remotePlayerPrefab == null)
        {
            Debug.LogError("[PlayerManager] Remote player prefab not assigned");
            return;
        }

        Vector3 pos = transform.ToPosition();
        Quaternion rot = transform.ToRotation();

        var playerGO = Instantiate(remotePlayerPrefab, pos, rot);

        var remoteCtrl = playerGO.GetComponent<RemotePlayerController>();
        remoteCtrl?.Initialize(defaultMoveSpeed, defaultRotateSpeed);

        remotePlayers.Add(playerId, playerGO);
    }
}