using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    private Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();
    private HashSet<int> selectedCharacters = new HashSet<int>();
    private Dictionary<int, int> playerBombCounts = new Dictionary<int, int>(); // Store bomb counts per player

    [Header("Bomb Settings")]
    public int defaultBombAmount = 1;

    [Header("Character Selection")]
    public GameObject[] joinerPrefabs;
    public Button[] characterButtons;

    [Header("Spawn System")]
    public List<Transform> spawnPoints;

    [Header("UI Controls")]
    public Joystick joystick;
    public Button bombButton;

    [Header("Destructible")]
    public Tilemap destructibleTiles;
    public Destructible destructiblePrefab;

    [Header("Explosion Settings")]
    [SerializeField] private Explosion explosionPrefab;
    [SerializeField] private float explosionDuration = 1f;
    [SerializeField] private int explosionRadius = 1;
    public LayerMask explosionLayerMask;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DestroyImmediate(gameObject); // Destroy duplicate instances
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Ensure it persists across scenes
    }

    public void SetBombAmountForPlayer(int playerID, int amount)
    {
        if (playerBombCounts.ContainsKey(playerID))
        {
            playerBombCounts[playerID] = amount;
        }
        else
        {
            playerBombCounts.Add(playerID, amount);
        }
    }

    public int GetBombAmountForPlayer(int playerID)
    {
        if (playerBombCounts.ContainsKey(playerID))
        {
            return playerBombCounts[playerID];
        }
        return defaultBombAmount;
    }

    public void IncreaseBombAmountForPlayer(int playerID)
    {
        if (playerBombCounts.ContainsKey(playerID))
        {
            playerBombCounts[playerID]++;
        }
    }

    public void DecreaseBombAmountForPlayer(int playerID)
    {
        if (playerBombCounts.ContainsKey(playerID))
        {
            playerBombCounts[playerID]--;
        }
    }

    public void IncreaseBombAmount()
    {
        defaultBombAmount++;
    }

    public void IncreaseExplosionRadius()
    {
        explosionRadius++;
    }

    public void OnCharacterSelected(int characterIndex)
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError("[GameManager] Not in a Photon room!");
            return;
        }

        if (selectedCharacters.Contains(characterIndex))
        {
            Debug.LogError($"[GameManager] Character {characterIndex} already taken!");
            return;
        }

        selectedCharacters.Add(characterIndex);
        photonView.RPC(nameof(RPC_DisableButton), RpcTarget.AllBuffered, characterIndex);
        SpawnPlayer(characterIndex);
    }

    private void SpawnPlayer(int characterIndex)
    {
        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;

        if (spawnedPlayers.ContainsKey(playerId))
        {
            Debug.LogWarning($"[GameManager] Player {playerId} already spawned.");
            return;
        }

        if (characterIndex < 0 || characterIndex >= joinerPrefabs.Length)
        {
            Debug.LogError($"[GameManager] Invalid character index: {characterIndex}");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogError("[GameManager] No spawn points set!");
            return;
        }

        int spawnIndex = (playerId - 1) % spawnPoints.Count;
        Vector3 spawnPosition = spawnPoints[spawnIndex].position;

        GameObject playerPrefab = joinerPrefabs[characterIndex];
        if (playerPrefab == null)
        {
            Debug.LogError($"[GameManager] No prefab assigned for character {characterIndex}");
            return;
        }

        GameObject newPlayer = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
        if (newPlayer == null)
        {
            Debug.LogError("[GameManager] Failed to instantiate player prefab!");
            return;
        }

        spawnedPlayers[playerId] = newPlayer;
        AssignControls(newPlayer);

        Debug.Log($"[GameManager] Player {playerId} spawned as character {characterIndex} at {spawnPosition}");
    }

    // Refactored explosion function that handles network syncing
    public void HandleExplosion(Vector2 position)
    {
        // Instantiate the explosion at the bomb position
        GameObject explosionObj = PhotonNetwork.Instantiate(explosionPrefab.name, position, Quaternion.identity);
        Explosion explosion = explosionObj.GetComponent<Explosion>();
        explosion.SetActiveRenderer(explosion.start);
        explosion.DestroyAfter(explosionDuration);

        // Call the Explode function for all directions
        Explode(position, Vector2.up, explosionRadius);
        Explode(position, Vector2.down, explosionRadius);
        Explode(position, Vector2.left, explosionRadius);
        Explode(position, Vector2.right, explosionRadius);
    }

    private void Explode(Vector2 position, Vector2 direction, int length)
    {
        if (length <= 0) return;

        // Iterate through all explosion lengths in the specified direction
        for (int i = 1; i <= length; i++)
        {
            // Move the position by the direction each time
            position += direction;

            // Check for destructible and indestructible tiles
            Collider2D hit = Physics2D.OverlapBox(position, Vector2.one / 2, 0f, explosionLayerMask);
            if (hit != null)
            {
                if (hit.gameObject.layer == LayerMask.NameToLayer("Destructible"))
                {
                    ClearDestructibleTile(position); // Clear destructible tile
                }
                else if (hit.gameObject.layer == LayerMask.NameToLayer("Indestructible"))
                {
                    return; // Stop explosion if it hits indestructible tiles
                }
            }

            // Visualize the explosion in the current position (adjust this as needed)
            GameObject explosionObj = PhotonNetwork.Instantiate(explosionPrefab.name, position, Quaternion.identity);
            Explosion explosion = explosionObj.GetComponent<Explosion>();
            explosion.SetActiveRenderer(explosion.middle); // Make middle part active for all other explosion positions
            explosion.SetDirection(direction); // Set explosion direction
            explosion.DestroyAfter(explosionDuration);
        }
    }


    // New method to clear destructible tiles
    public void ClearDestructibleTile(Vector2 position)
    {
        Vector3Int cell = destructibleTiles.WorldToCell(position);
        TileBase tile = destructibleTiles.GetTile(cell);

        if (tile != null)
        {
            photonView.RPC(nameof(RPCClearDestructible), RpcTarget.All, position);
        }
    }

    [PunRPC]
    private void RPCClearDestructible(Vector2 position)
    {
        Vector3Int cell = destructibleTiles.WorldToCell(position);
        TileBase tile = destructibleTiles.GetTile(cell);

        if (tile != null)
        {
            // Instantiate the destructible prefab (if needed)
            PhotonNetwork.Instantiate(destructiblePrefab.name, position, Quaternion.identity);
            destructibleTiles.SetTile(cell, null);
        }
    }


    [PunRPC]
    private void RPC_DisableButton(int characterIndex)
    {
        if (characterIndex < 0 || characterIndex >= characterButtons.Length)
            return;

        if (!selectedCharacters.Contains(characterIndex))
            selectedCharacters.Add(characterIndex);

        if (characterButtons[characterIndex] != null)
        {
            characterButtons[characterIndex].interactable = false;
        }
        else
        {
            Debug.LogError($"[GameManager] Character button {characterIndex} is missing in the UI.");
        }
    }

    private void AssignControls(GameObject player)
    {
        if (player == null)
        {
            Debug.LogError("[GameManager] Player object is null, cannot assign controls.");
            return;
        }

        MovementController movementController = player.GetComponent<MovementController>();
        BombController bombController = player.GetComponent<BombController>();

        if (movementController != null)
        {
            movementController.joystick = joystick;

            // Initialize the sprite renderers
            AnimatedSpriteRenderer[] spriteRenderers = player.GetComponentsInChildren<AnimatedSpriteRenderer>();
            foreach (var renderer in spriteRenderers)
            {
                renderer.idle = true; // Set to idle initially
            }
        }
        else
        {
            Debug.LogError($"[GameManager] MovementController missing on {player.name}");
        }

        if (bombController != null)
        {
            bombController.bombButton = bombButton;
        }
        else
        {
            Debug.LogError($"[GameManager] BombController missing on {player.name}");
        }
    }

    public void CheckWinState()
    {
        int aliveCount = 0;

        foreach (var player in spawnedPlayers.Values)
        {
            if (player != null && player.activeSelf)
                aliveCount++;
        }

        if (aliveCount <= 1)
        {
            photonView.RPC(nameof(NewRound), RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    private void NewRound()
    {
        spawnedPlayers.Clear();
        selectedCharacters.Clear();

        // Synchronize the scene reload for all players in the room
        PhotonNetwork.LoadLevel("Lobby");
    }
}