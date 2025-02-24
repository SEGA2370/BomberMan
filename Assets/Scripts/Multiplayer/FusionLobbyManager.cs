using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class FusionLobbyManager : MonoBehaviour
{
    public NetworkRunner runner;  // Assign via Inspector if not on the same GameObject.

    [Header("UI Elements")]
    public TMP_InputField sessionNameInput;
    public Button createRoomButton;
    public Button joinRoomButton;

    // Name of the gameplay scene (must be added to Build Settings)
    public string gameSceneName = "Game";

    private void Start()
    {
        if (runner == null)
            runner = GetComponent<NetworkRunner>();

        createRoomButton.onClick.AddListener(CreateRoom);
        joinRoomButton.onClick.AddListener(JoinRoom);
    }

    public async void CreateRoom()
    {
        string sessionName = sessionNameInput.text;
        if (string.IsNullOrEmpty(sessionName))
            sessionName = "DefaultRoom";

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            // Use default(NetworkSceneInfo) so that the current scene is used (or let your SceneManager handle the transition)
            Scene = default(NetworkSceneInfo),
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
        };

        var result = await runner.StartGame(startGameArgs);
        if (result.Ok)
        {
            Debug.Log("Room created successfully!");
            await runner.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Room creation failed: " + result.ShutdownReason);
        }
    }

    public async void JoinRoom()
    {
        string sessionName = sessionNameInput.text;
        if (string.IsNullOrEmpty(sessionName))
            sessionName = "DefaultRoom";

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            Scene = default(NetworkSceneInfo),
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
        };

        var result = await runner.StartGame(startGameArgs);
        if (result.Ok)
        {
            Debug.Log("Joined room successfully!");
        }
        else
        {
            Debug.LogError("Joining room failed: " + result.ShutdownReason);
        }
    }
}
