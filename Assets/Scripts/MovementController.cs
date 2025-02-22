using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Added for UI Joystick and Buttons
using Photon.Pun;

public class MovementController : MonoBehaviourPunCallbacks
{
    private new Rigidbody2D rigidbody;
    private Vector2 direction = Vector2.down;
    public float speed = 5f;

    [Header("Input")]
    public KeyCode inputUp = KeyCode.W;
    public KeyCode inputDown = KeyCode.S;
    public KeyCode inputLeft = KeyCode.A;
    public KeyCode inputRight = KeyCode.D;

    [Header("Sprites")]
    [SerializeField] private AnimatedSpriteRenderer spriteRendererUp;
    [SerializeField] private AnimatedSpriteRenderer spriteRendererDown;
    [SerializeField] private AnimatedSpriteRenderer spriteRendererLeft;
    [SerializeField] private AnimatedSpriteRenderer spriteRendererRight;
    [SerializeField] private AnimatedSpriteRenderer spriteRendererDeath;
    private AnimatedSpriteRenderer activeSpriteRenderer;

#if UNITY_ANDROID || UNITY_IOS
    [Header("Mobile Controls")]
    public Joystick joystick; // UI Joystick for movement
#endif

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        activeSpriteRenderer = spriteRendererDown;
    }

    private void Update()
    {
        if (photonView.IsMine) // Ensure only the local player can control movement
        {
            Movement();
        }
    }

    public void Movement()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (joystick != null)
        {
            direction = new Vector2(joystick.Horizontal, joystick.Vertical);

            if (direction.sqrMagnitude > 0.1f) // Check if joystick is being moved
            {
                Vector2 normalizedDirection = direction.normalized;

                if (Mathf.Abs(normalizedDirection.x) > Mathf.Abs(normalizedDirection.y))
                {
                    // Moving horizontally
                    if (normalizedDirection.x > 0)
                        SetDirection(Vector2.right, spriteRendererRight);
                    else
                        SetDirection(Vector2.left, spriteRendererLeft);
                }
                else
                {
                    // Moving vertically
                    if (normalizedDirection.y > 0)
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
#else
        if (Input.GetKey(inputUp))
        {
            SetDirection(Vector2.up, spriteRendererUp);
        }
        else if (Input.GetKey(inputDown))
        {
            SetDirection(Vector2.down, spriteRendererDown);
        }
        else if (Input.GetKey(inputLeft))
        {
            SetDirection(Vector2.left, spriteRendererLeft);
        }
        else if (Input.GetKey(inputRight))
        {
            SetDirection(Vector2.right, spriteRendererRight);
        }
        else
        {
            SetDirection(Vector2.zero, activeSpriteRenderer);
        }
#endif
    }

    private void FixedUpdate()
    {
        Vector2 position = rigidbody.position;
        Vector2 translation = speed * Time.fixedDeltaTime * direction;

        rigidbody.MovePosition(position + translation);
    }

    private void SetDirection(Vector2 newDirection, AnimatedSpriteRenderer spriteRenderer)
    {
        if (direction != newDirection || activeSpriteRenderer != spriteRenderer)
        {
            direction = newDirection;

            // Disable all sprite renderers except the active one
            spriteRendererUp.enabled = spriteRenderer == spriteRendererUp;
            spriteRendererDown.enabled = spriteRenderer == spriteRendererDown;
            spriteRendererLeft.enabled = spriteRenderer == spriteRendererLeft;
            spriteRendererRight.enabled = spriteRenderer == spriteRendererRight;

            activeSpriteRenderer = spriteRenderer;
            activeSpriteRenderer.idle = direction == Vector2.zero;

            // Send RPC to sync the animation state
            photonView.RPC(nameof(SyncAnimationState), RpcTarget.All, newDirection, spriteRenderer.name);
        }
    }

    [PunRPC]
    private void SyncAnimationState(Vector2 newDirection, string spriteName)
    {
        // Find the target sprite renderer based on the spriteName received
        AnimatedSpriteRenderer targetSprite = spriteName switch
        {
            nameof(spriteRendererUp) => spriteRendererUp,
            nameof(spriteRendererDown) => spriteRendererDown,
            nameof(spriteRendererLeft) => spriteRendererLeft,
            nameof(spriteRendererRight) => spriteRendererRight,
            _ => activeSpriteRenderer
        };

        // Update the direction and sprite renderer
        direction = newDirection;
        activeSpriteRenderer = targetSprite;
        activeSpriteRenderer.idle = direction == Vector2.zero;

        // Disable all sprite renderers except the active one
        spriteRendererUp.enabled = targetSprite == spriteRendererUp;
        spriteRendererDown.enabled = targetSprite == spriteRendererDown;
        spriteRendererLeft.enabled = targetSprite == spriteRendererLeft;
        spriteRendererRight.enabled = targetSprite == spriteRendererRight;
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
        FindObjectOfType<GameManager>().CheckWinState();
    }
}
