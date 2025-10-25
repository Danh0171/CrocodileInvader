using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrackedRoad : PoolableObject
{
    [Header("CrackedRoad Components")]
    [SerializeField] private Surface surface;
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    
    [Header("Auto Crack Settings")]
    [SerializeField] private float fallSpeed = 1.5f;
    
    private float delayBeforeCrack;
    private float crackTime;
    
    private bool isCracking = false;
    private bool hasFallen = false;
    private float crackTimer = 0f;
    private float delayTimer = 0f;
    
    public override float Width => spriteRenderer.size.x;
    public override float Height => spriteRenderer.size.y;
    
    public void Config(float width, int layer)
    {
        float crackedRoadWidth = width * 1.3f; // 30% longer than normal road
        
        spriteRenderer.size = new Vector2(crackedRoadWidth, spriteRenderer.size.y);
        surface.Resize(crackedRoadWidth); // Resize surface colliders 
        boxCollider.size = new Vector2(crackedRoadWidth, boxCollider.size.y);
        spriteRenderer.sortingOrder = layer;
        
        // Calculate timing based on ORIGINAL width (not extended)
        CalculateCrackTiming(width);
        
    }
    
    private void CalculateCrackTiming(float width)
    {
        float currentScrollSpeed = GameManager.Instance.ScrollBackSpeed;
        
        // Delay before crack starts (safe time)
        delayBeforeCrack = ((width - 4.4f)*2f ) / (currentScrollSpeed * 5f - 49.3f); 
        
        crackTime = delayBeforeCrack * 0.55f; 
        
        delayBeforeCrack = Mathf.Max(delayBeforeCrack, 0.1f); 
        crackTime = Mathf.Max(crackTime, 1f); 
    }

    protected override void Start()
    {
        
    }

    protected override void Update()
    {
        base.Update();
        
        if (!isCracking && !hasFallen)
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= delayBeforeCrack)
            {
                StartCracking();
            }
        }
        else if (isCracking && !hasFallen)
        {
            crackTimer += Time.deltaTime;
            if (crackTimer >= crackTime)
            {
                BreakRoad();
            }
        }
        
        if (hasFallen)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        }
    }

    public override void Init()
    {
        base.Init();
        isCracking = false;
        hasFallen = false;
        crackTimer = 0f;
        delayTimer = 0f; 
        if (animator != null)
        {
            animator.SetBool("isCracking", false);
            animator.ResetTrigger("break");
        }
        
    }

    private void StartCracking()
    {
        if (!isCracking)
        {
            isCracking = true;
            crackTimer = 0f;
            if (animator != null)
                animator.SetBool("isCracking", true);
            
            GameplayMusicManager.Instance.PlayCrackedRoadSound();
        }
    }

    private void BreakRoad()
    {
        hasFallen = true;
        
        if (animator != null)
            animator.SetTrigger("break");
    }
}
