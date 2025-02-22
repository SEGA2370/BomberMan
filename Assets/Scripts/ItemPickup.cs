using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public enum ItemType
    {
        ExtraBomb,
        BlastRadius,
        SpeedIncrease,
    }

    public ItemType type;

    private void OnItemPickup(GameObject player)
    {
        // Check if the player is active before applying the item effect
        if (player != null && player.activeSelf)
        {
            switch (type)
            {
                case ItemType.ExtraBomb:
                    GameManager.Instance.IncreaseBombAmount();
                    break;

                case ItemType.BlastRadius:
                    GameManager.Instance.IncreaseExplosionRadius();
                    break;

                case ItemType.SpeedIncrease:
                    player.GetComponent<MovementController>().speed++;
                    break;
            }

            // Destroy the item after pickup
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            OnItemPickup(other.gameObject);
        }
    }
}
