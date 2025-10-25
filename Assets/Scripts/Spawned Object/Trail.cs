using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trail : PoolableObject
{
    [Header("Trail Settings")]
    [SerializeField] private float orbitSpeed = 90f; 
    [SerializeField] private float baseOrbitRadius = 1f; 
    [SerializeField] private float radiusMultiplier = 0.1f; 
    [SerializeField] private float yDampingSpeed = 5f;
    
    [Header("Trail Magic Effects")]
    [SerializeField] private TrailType trailType = TrailType.Fire; 
    [SerializeField] private float destroyChance = 0.1f; 
    
    [Header("Effect Sprites")]
    [SerializeField] private Sprite effectSprite;
    
    private Vector3 centerPosition; 
    private TrailRenderer trailRenderer;
    private float currentY; 
    private Collider2D trailCollider; 
    
    public enum TrailType
    {
        Fire,    
        Ice,      
        Nature,  
        Dark     
    }

    public override float Width => trailCollider ? trailCollider.bounds.size.x : 1f;
    public override float Height => trailCollider ? trailCollider.bounds.size.y : 1f;
    
    public TrailType Type => trailType;
    public Sprite EffectSprite => effectSprite;

    protected override void Start()
    {
        base.Start();
        
        // Lưu vị trí ban đầu làm center (chỉ X và Z)
        centerPosition = transform.localPosition;
        currentY = centerPosition.y; // Khởi tạo current Y
        
        trailRenderer = GetComponent<TrailRenderer>();
        trailCollider = GetComponent<Collider2D>();
    }

    public override void Init()
    {
        base.Init();
        
        centerPosition = transform.localPosition;
        currentY = centerPosition.y;
    }

    protected override void Update()
    {
        OrbitMovement();
        
        if (GameManager.Instance.Zombies.FirstDragon == null)
        {
            DestroyOnOutOfBounds();
        }
    }

    private void OrbitMovement()
    {
        float dynamicRadius = CalculateDynamicRadius();
        
        Vector3 dynamicCenter = CalculateDynamicCenter();
        
        float targetY = CalculateDragonY();
        currentY = Mathf.Lerp(currentY, targetY, yDampingSpeed * Time.deltaTime);
        
        float currentOrbitSpeed = orbitSpeed * GameManager.Instance.ScrollBackSpeed;
        float angle = Time.time * currentOrbitSpeed * Mathf.Deg2Rad;
        
        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(angle) * dynamicRadius,
            0, 
            Mathf.Sin(angle) * dynamicRadius * 0.5f 
        );
        
        Vector3 newPosition = dynamicCenter + orbitOffset;
        newPosition.y = currentY + 0.7f; 
        
        if (GameManager.Instance.Zombies.FirstDragon != null)
        {
            float dragonZ = GameManager.Instance.Zombies.FirstDragon.transform.position.z;
            newPosition.z = dragonZ + orbitOffset.z; 
            
            UpdateSortingOrder(orbitOffset.z);
        }
        
        transform.localPosition = newPosition;
    }
    
    private void UpdateSortingOrder(float zOffset)
    {
        if (trailRenderer == null) return;
        
        if (zOffset > 0)
        {
            // Trail ở phía trước
            trailRenderer.sortingLayerName = "UI"; 
        }
        else
        {
            // Trail ở phía sau  
            trailRenderer.sortingLayerName = "Vehicle"; 
        }
    }
    
    private float CalculateDynamicRadius()
    {
        DragonManager dragonManager = GameManager.Instance.Zombies;
        
        if (dragonManager == null || dragonManager.Count == 0)
            return baseOrbitRadius;
        
        float countBasedRadius = baseOrbitRadius + (dragonManager.Count * radiusMultiplier);
        
        return countBasedRadius;
    }
    
    private Vector3 CalculateDynamicCenter()
    {
        DragonManager dragonManager = GameManager.Instance.Zombies;
        
        if (dragonManager == null || dragonManager.FirstDragon == null)
            return centerPosition; 
        
        Vector3 firstDragonPos = dragonManager.FirstDragon.transform.position;
        
        // Tính toán offset dựa trên số lượng dragons
        float baseOffset = 0.1f; 
        float countOffset = dragonManager.Count * 0.2f; 
        float totalOffset = baseOffset + countOffset;
        
        Vector3 dynamicCenter = new Vector3(
            firstDragonPos.x - totalOffset,
            centerPosition.y, 
            firstDragonPos.z
        );
        
        return dynamicCenter;
    }
    
    private float CalculateDragonY()
    {
        DragonManager dragonManager = GameManager.Instance.Zombies;
        
        if (dragonManager == null || dragonManager.FirstDragon == null)
            return centerPosition.y; 
        
        // Sync với Y của dragon đầu tiên
        return dragonManager.FirstDragon.transform.position.y;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (Random.Range(0f, 1f) <= destroyChance)
        {
            ApplyTrailEffect(other.gameObject);
        }
    }
    
    private void ApplyTrailEffect(GameObject target)
    {
        if (target == null) return;
        
        if (!CanDestroyObject(target)) return;
        
        // Check nếu object đã bị marked for destruction
        if (!target.activeInHierarchy) return;
        
        ApplyEffectToTarget(target, effectSprite);
    }
    
    private bool CanDestroyObject(GameObject target)
    {
        // chỉ phá được certain objects
        string tag = target.tag;
        return tag == "Object" || tag == "Bomb" || tag == "Box" || tag == "Obstacle";
    }
    
    private void ApplyEffectToTarget(GameObject target, Sprite effectSprite)
    {
        if (target == null) return;
        
        CreateLeafSprite(target.transform.position, effectSprite);
        
        // Special handling for Box - generate prey trước khi biến mất
        if (target.CompareTag("Box"))
        {
            Box box = target.GetComponent<Box>();
            if (box != null)
            {
                // Call GeneratePreys method through reflection since it's private
                var generatePreysMethod = box.GetType().GetMethod("GeneratePreys", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                generatePreysMethod?.Invoke(box, null);
            }
        }
        
        ReturnToPool(target);
    }

    private void ReturnToPool(GameObject target)
    {
        if (target == null) return;
        
        if (target.CompareTag("Box"))
        {
            Box box = target.GetComponent<Box>();
            if (box != null)
            {
                var generatePreysMethod = box.GetType().GetMethod("GeneratePreys", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                generatePreysMethod?.Invoke(box, null);
            }
        }
        
        PoolableObject poolable = target.GetComponent<PoolableObject>();
        if (poolable != null)
        {
            var removeSelfMethod = poolable.GetType().GetMethod("RemoveSelf", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            removeSelfMethod?.Invoke(poolable, null);
        }
        else
        {
            target.SetActive(false);
        }
    }
    
    private void CreateLeafSprite(Vector3 position, Sprite effectSprite)
    {
        // Tạo GameObject mới cho effect sprite
        GameObject effectObject = new GameObject("EffectSprite");
        effectObject.transform.position = position;
        
        // Thêm SpriteRenderer component
        SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        
        if (effectSprite != null)
        {
            effectRenderer.sprite = effectSprite;
        }
        
        effectRenderer.sortingLayerName = "UI";
        effectRenderer.sortingOrder = 10;
        
        StartCoroutine(LeafFallAnimation(effectObject));
    }
    
    private System.Collections.IEnumerator LeafFallAnimation(GameObject leafObject)
    {
        if (leafObject == null) yield break;
        
        Vector3 startPosition = leafObject.transform.position;
        SpriteRenderer renderer = leafObject.GetComponent<SpriteRenderer>();
        
        float duration = 2f;
        float elapsed = 0f;
        
        while (elapsed < duration && leafObject != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Animation trôi về bên trái với tốc độ ScrollBackSpeed
            if (GameManager.Instance != null)
            {
                float moveSpeed = GameManager.Instance.ScrollBackSpeed;
                Vector3 currentPos = leafObject.transform.position;
                currentPos.x -= moveSpeed * Time.deltaTime;
                leafObject.transform.position = currentPos;
            }
            
            // Fade out trong nửa cuối của animation
            if (progress > 0.5f && renderer != null)
            {
                float fadeProgress = (progress - 0.5f) * 2f; 
                Color color = renderer.color;
                color.a = 1f - fadeProgress;
                renderer.color = color;
            }
            
            yield return null;
        }
        
        if (leafObject != null)
        {
            Destroy(leafObject);
        }
    }

    protected override void DestroyOnOutOfBounds()
    {
        if (GameManager.Instance.Zombies.FirstDragon == null)
        {
            base.DestroyOnOutOfBounds();
        }
    }
}