using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedSpriteRenderer : MonoBehaviourPunCallbacks, IPunObservable
{
    private SpriteRenderer spriteRenderer;

    public Sprite idleSprite;
    public Sprite[] animationSprites;

    public float animationTime = 0.25f;
    private int animationFrame;

    public bool loop = true;
    public bool idle = true;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private new void OnEnable()
    {
        spriteRenderer.enabled = true;
    }

    private new void OnDisable()
    {
        spriteRenderer.enabled = false;
    }

    private void Start()
    {
        if (photonView.IsMine) // Only the local player controls the animation
        {
            InvokeRepeating(nameof(NextFrame), animationTime, animationTime);
        }
    }

    private void NextFrame()
    {
        if (!idle) // Only animate if not idle
        {
            animationFrame++;

            if (loop && animationFrame >= animationSprites.Length)
            {
                animationFrame = 0;
            }

            if (animationFrame >= 0 && animationFrame < animationSprites.Length)
            {
                spriteRenderer.sprite = animationSprites[animationFrame];
            }
        }
        else
        {
            spriteRenderer.sprite = idleSprite; // Ensure idle sprite is shown when not moving
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Send animation state to other players
            stream.SendNext(animationFrame);
            stream.SendNext(idle);
        }
        else
        {
            // Receive animation state from the owner
            animationFrame = (int)stream.ReceiveNext();
            idle = (bool)stream.ReceiveNext();

            // Update the sprite based on the received data
            if (idle)
            {
                spriteRenderer.sprite = idleSprite;
            }
            else
            {
                if (animationFrame >= 0 && animationFrame < animationSprites.Length)
                {
                    spriteRenderer.sprite = animationSprites[animationFrame];
                }
            }
        }
    }

}
