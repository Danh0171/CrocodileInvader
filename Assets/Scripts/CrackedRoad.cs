using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrackedRoad : PoolableObject
{
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private float crackTime = 2f; // Thời gian trước khi vỡ
    [SerializeField] private float fallSpeed = 5f;
    
    private bool isCracking = false;
    private bool hasFallen = false;
    private float crackTimer = 0f;
    private List<Crocodile> crocodilesOnRoad = new List<Crocodile>();
    
    public override float Width => boxCollider.size.x;
    public override float Height => boxCollider.size.y;

    protected override void Start()
    {
        
    }

    protected override void Update()
    {
        base.Update();
        
        if (isCracking && !hasFallen)
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
        crocodilesOnRoad.Clear();
        boxCollider.isTrigger = false;
        if (animator != null)
            animator.SetBool("isCracking", false);
    }

    private void StartCracking()
    {
        if (!isCracking)
        {
            isCracking = true;
            crackTimer = 0f;
            if (animator != null)
                animator.SetBool("isCracking", true);
            
            // Play crack sound effect
            GameplayMusicManager.Instance.PlayBoomSound();
        }
    }

    private void BreakRoad()
    {
        hasFallen = true;
        boxCollider.isTrigger = true;
        
        if (animator != null)
            animator.SetTrigger("break");
            
        // Make crocodiles fall
        foreach (var crocodile in crocodilesOnRoad)
        {
            if (crocodile != null)
                crocodile.CallTriggerFall(0.5f);
        }
        
        // Play break sound
        GameplayMusicManager.Instance.PlayCarExplodeSound();
        GameManager.Instance.CallExplosion(false, transform.position);
        
        // Remove after delay
        StartCoroutine(RemoveAfterDelay(2f));
    }
    
    private IEnumerator RemoveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveSelf();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Zombie"))
        {
            var crocodile = collision.gameObject.GetComponent<Crocodile>();
            if (crocodile != null && !crocodilesOnRoad.Contains(crocodile))
            {
                crocodilesOnRoad.Add(crocodile);
                StartCracking();
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Zombie"))
        {
            var crocodile = collision.gameObject.GetComponent<Crocodile>();
            if (crocodile != null && crocodilesOnRoad.Contains(crocodile))
            {
                crocodilesOnRoad.Remove(crocodile);
            }
        }
    }
}
