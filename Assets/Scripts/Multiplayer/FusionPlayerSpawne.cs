using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System;
using System.Collections.Generic;

public class FusionPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkRunner runner;
    // Must match the name of your Player prefab in Resources.
    public string playerPrefabName = "Player";

    // Fixed spawn positions for 4 players:
    // Index 0: (-6, 5, 0), Index 1: (6, -5, 0),
    // Index 2: (6, 5, 0), Index 3: (-6, -5, 0)
    private readonly Vector3[] spawnPositions = new Vector3[]
    {
        new Vector3(-6f, 5f, 0f),
        new Vector3(6f, -5f, 0f),
        new Vector3(6f, 5f, 0f),
        new Vector3(-6f, -5f, 0f)
    };

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            int index = (int)player.RawEncoded;
            if (index < 0 || index >= spawnPositions.Length)
            {
                Debug.LogWarning("Player index out of bounds or max players reached. Not spawning player.");
                return;
            }
            Vector3 spawnPosition = spawnPositions[index];

            string prefabName = "";
            switch (index)
            {
                case 0:
                    prefabName = "Player";
                    break;
                case 1:
                    prefabName = "Player2";
                    break;
                case 2:
                    prefabName = "Player3";
                    break;
                case 3:
                    prefabName = "Player4";
                    break;
                default:
                    Debug.LogWarning("Invalid player index. Not spawning player.");
                    return;
            }

            MovementController playerPrefab = Resources.Load<MovementController>(prefabName);
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab not found in Resources with name: " + prefabName);
                return;
            }

            runner.Spawn<MovementController>(playerPrefab, spawnPosition, Quaternion.identity, player);
        }
    }


    // Explicit interface implementations to satisfy INetworkRunnerCallbacks.
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
}
