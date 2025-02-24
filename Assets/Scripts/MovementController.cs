using Fusion;
using UnityEngine;

public class MovementController : NetworkBehaviour
{
    private Rigidbody2D rb;
    private Vector2 direction = Vector2.down;
    public float speed = 5f;

    [Header("Joystick Input")]
    public FixedJoystick fixedJoystick;

    [Header("Sprites")]
    public AnimatedSpriteRenderer spriteRendererUp;
    public AnimatedSpriteRenderer spriteRendererDown;
    public AnimatedSpriteRenderer spriteRendererLeft;
    public AnimatedSpriteRenderer spriteRendererRight;
    public AnimatedSpriteRenderer spriteRendererDeath;
    private AnimatedSpriteRenderer activeSpriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        activeSpriteRenderer = spriteRendererDown;      
    }

    private void Start()
    {
        if (fixedJoystick == null)
        {
            fixedJoystick = FindObjectOfType<FixedJoystick>();
            if (fixedJoystick == null)
            {
                Debug.LogError("FixedJoystick not found in the scene!");
            }
        }
    }

    private void Update()
    {
        // Process input only if this is the local player.
        if (!Object.HasInputAuthority)
            return;
        if (fixedJoystick == null)
        {
            Debug.LogWarning("FixedJoystick is not assigned.");
            return;
        }

        Vector2 joystickInput = new Vector2(fixedJoystick.Horizontal, fixedJoystick.Vertical);

        if (joystickInput.sqrMagnitude > 0.1f)
        {
            direction = joystickInput.normalized;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                if (direction.x > 0)
                    SetDirection(Vector2.right, spriteRendererRight);
                else
                    SetDirection(Vector2.left, spriteRendererLeft);
            }
            else
            {
                if (direction.y > 0)
                    SetDirection(Vector2.up, spriteRendererUp);
                else
                    SetDirection(Vector2.down, spriteRendererDown);
            }
        }
        else
        {
            SetDirection(Vector2.zero, activeSpriteRenderer);
        }
    }

    private void FixedUpdate()
    {
        Vector2 position = rb.position;
        Vector2 translation = speed * Time.fixedDeltaTime * direction;
        rb.MovePosition(position + translation);
    }

    private void SetDirection(Vector2 newDirection, AnimatedSpriteRenderer spriteRenderer)
    {
        direction = newDirection;
        spriteRendererUp.enabled = spriteRenderer == spriteRendererUp;
        spriteRendererDown.enabled = spriteRenderer == spriteRendererDown;
        spriteRendererLeft.enabled = spriteRenderer == spriteRendererLeft;
        spriteRendererRight.enabled = spriteRenderer == spriteRendererRight;
        activeSpriteRenderer = spriteRenderer;
        activeSpriteRenderer.idle = direction == Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
        {
            DeathSequence();
        }
    }

    private void DeathSequence()
    {
        enabled = false;
        GetComponent<BombController>().enabled = false;
        spriteRendererUp.enabled = false;
        spriteRendererDown.enabled = false;
        spriteRendererLeft.enabled = false;
        spriteRendererRight.enabled = false;
        spriteRendererDeath.enabled = true;
        Invoke(nameof(OnDeathSequenceEnded), 1.25f);
    }

    private void OnDeathSequenceEnded()
    {
        gameObject.SetActive(false);
        GameManager.Instance.CheckWinState();
    }
}
