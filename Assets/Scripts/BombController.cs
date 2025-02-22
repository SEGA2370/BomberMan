using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BombController : MonoBehaviourPun
{
    [Header("Bomb")]
    [SerializeField] GameObject bombPrefab;
    public KeyCode inputKey = KeyCode.Space;
    [SerializeField] float bombFuseTime = 3f;
   

#if UNITY_ANDROID || UNITY_IOS
    [Header("Mobile Controls")]
    public Button bombButton; // UI Button for bomb placement
#endif

    private void OnEnable()
    {
        if (photonView.IsMine)
        {
            GameManager.Instance.SetBombAmountForPlayer(photonView.OwnerActorNr, GameManager.Instance.defaultBombAmount);
        }

#if UNITY_ANDROID || UNITY_IOS
        // Add listener for bomb button press on mobile platforms
        if (photonView.IsMine && bombButton != null)
        {
            bombButton.onClick.AddListener(() =>
            {
                Debug.Log("Bomb Button Pressed!");
                StartCoroutine(PlaceBomb());
            });
        }
#endif
    }

    private void Update()
    {
        // Ensure only the local player can place bombs
        if (!photonView.IsMine) return;

#if !UNITY_ANDROID && !UNITY_IOS
        // Handle bomb placement through keyboard on non-mobile platforms
        if (GameManager.Instance.GetdefaultBombAmountForPlayer(photonView.OwnerActorNr) > 0 && Input.GetKeyDown(inputKey))
        {
            Debug.Log("Bomb Key Pressed");
            StartCoroutine(PlaceBomb());
        }
#endif
    }

    // Place bombs using photon network to synchronize across players
    public void PlaceBombs()
    {
        if (!photonView.IsMine) return; // Prevent non-owners from placing bombs
        StartCoroutine(PlaceBomb());
    }

    private IEnumerator PlaceBomb()
    {
        if (!photonView.IsMine) yield break; // Prevent non-owners from executing this method

        int currentBombAmount = GameManager.Instance.GetBombAmountForPlayer(photonView.OwnerActorNr);
        Debug.Log("PlaceBomb Coroutine Started");

        // Check if there are bombs remaining
        if (currentBombAmount <= 0)
        {
            Debug.Log("No Bombs Remaining!");
            yield break;
        }
        // Decrease bombs remaining for this player
        GameManager.Instance.DecreaseBombAmountForPlayer(photonView.OwnerActorNr);

        // Get the player's position
        Vector2 position = GameObject.FindGameObjectWithTag("Player").transform.position;
        position.x = Mathf.Round(position.x);
        position.y = Mathf.Round(position.y);

        // Instantiate bomb over the network for all players
        GameObject bomb = PhotonNetwork.Instantiate(bombPrefab.name, position, Quaternion.identity);
        if (bomb == null)
        {
            Debug.LogError("Bomb Instantiation Failed!");
        }
        else
        {
            Debug.Log("Bomb Placed Successfully");
        }

        // Wait for the fuse time before triggering explosion
        yield return new WaitForSeconds(bombFuseTime);

        position = bomb.transform.position;
        position.x = Mathf.Round(position.x);
        position.y = Mathf.Round(position.y);

        // Notify GameManager to handle the explosion (Ensure it's synchronized across network)
        photonView.RPC("HandleExplosion", RpcTarget.All, position);

        // Destroy the bomb after explosion
        Destroy(bomb);
        GameManager.Instance.IncreaseBombAmountForPlayer(photonView.OwnerActorNr);  // Replenish bombs after explosion
    }   

        // This RPC will handle the explosion logic across all clients
    [PunRPC]
    private void HandleExplosion(Vector2 position)
    {
        // Handle explosion logic, such as damage, effects, etc.
        GameManager.Instance.HandleExplosion(position);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Bomb"))
        {
            other.isTrigger = false;
        }
    }
}
