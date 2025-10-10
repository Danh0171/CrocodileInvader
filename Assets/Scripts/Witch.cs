using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Witch : PoolableObject
{
    [Header("Witch Settings")]
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private float waitTime = 2f; // Đứng yên 2s
    [SerializeField] private float flySpeed = 20f; // Tốc độ bay ngang
    [SerializeField] private LayerMask dragonLayer; // Layer của dragons
    [SerializeField] private SpriteRenderer idleSprite; 
    [SerializeField] private SpriteRenderer flySprite; 
    private enum WitchState
    {
        Waiting,    // Đứng yên chờ
        Flying,     // Bay ngang
        Finished    // Đã bay xong
    }
    
    private WitchState currentState = WitchState.Waiting;
    private float stateTimer = 0f;
    
    public override float Width => boxCollider.size.x;
    public override float Height => boxCollider.size.y;

    protected override void Start()
    {
        base.Start();
    }

    public override void Init()
    {
        base.Init();
        GameplayMusicManager.Instance.PlayWitchIdleSound();
        idleSprite.enabled = true;
        flySprite.enabled = false;
        currentState = WitchState.Waiting;
        stateTimer = 0f;
        boxCollider.isTrigger = false; // Ban đầu không trigger
    }

    protected override void Update()
    {
        base.Update();
        
        switch (currentState)
        {
            case WitchState.Waiting:
                HandleWaitingState();
                break;
            case WitchState.Flying:
                HandleFlyingState();
                break;
            case WitchState.Finished:
                // Do nothing, waiting for removal
                break;
        }
    }
    
    private void HandleWaitingState()
    {
        stateTimer += Time.deltaTime;
        
        if (stateTimer >= waitTime)
        {
            GameplayMusicManager.Instance.PlayWitchFlySound();
            StartFlying();
        }
    }
    
    private void StartFlying()
    {
        currentState = WitchState.Flying;
        idleSprite.enabled = false;
        flySprite.enabled = true;
        stateTimer = 0f;
        boxCollider.isTrigger = true; // Bật trigger để kill dragons
    }
    
    private void HandleFlyingState()
    {
        // Bay sang trái với tốc độ flySpeed
        transform.position += Vector3.left * flySpeed * Time.deltaTime;
        
        // Kiểm tra nếu đã bay ra khỏi màn hình
        if (transform.position.x < -GameManager.ScreenWidth * 0.6f)
        {
            currentState = WitchState.Finished;
            RemoveSelf();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (currentState == WitchState.Flying && 
            collision.gameObject.CompareTag("Zombie"))
        {
            Dragon dragon = collision.gameObject.GetComponent<Dragon>();
            if (dragon != null)
            {
                KillDragon(dragon);
            }
        }
    }
    
    private void KillDragon(Dragon dragon)
    {
        // Effect khi giết dragon
        GameManager.Instance.CallExplosion(false, dragon.transform.position);
        GameplayMusicManager.Instance.PlayBoomSound();
        
        // Remove dragon using DragonManager (Zombies)
        GameManager.Instance.Zombies.ReturnItem(dragon);
    }
}
