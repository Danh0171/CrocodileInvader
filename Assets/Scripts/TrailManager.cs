using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class TrailManager : MonoBehaviour
{
    protected Manager manager;

    [Header("Orbit Settings")]
    [SerializeField] private float orbitSpeed = 90f; // Degrees per second
    [SerializeField] private float baseOrbitRadius = 1f; // Bán kính cơ bản
    [SerializeField] private float radiusMultiplier = 0.1f; // Hệ số nhân với số lượng/khoảng cách dragon
    [SerializeField] private float yDampingSpeed = 5f; // Tốc độ smooth Y position
    
    [Header("Trail Magic Effects")]
    [SerializeField] private TrailType trailType = TrailType.Fire; // Loại trail
    [SerializeField] private float destroyChance = 0.1f; // 10% chance phá vỡ
    [SerializeField] private float effectRadius = 1f; // Bán kính hiệu ứng
    
    [Header("Effect Sprites")]
    [SerializeField] private Sprite fireSprite; // Sprite cho Fire effect
    [SerializeField] private Sprite iceSprite; // Sprite cho Ice effect
    [SerializeField] private Sprite leafSprite; // Sprite cho Nature effect
    [SerializeField] private Sprite darkHoleSprite; // Sprite cho Dark effect
    
    // Dictionary để lưu trữ original values trước khi apply ice effect
    private Dictionary<GameObject, int> originalBoxDragonNeeded = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, bool> originalBombCollisionEnabled = new Dictionary<GameObject, bool>();
    
    // Dictionary để track effect sprites
    private Dictionary<GameObject, GameObject> effectSprites = new Dictionary<GameObject, GameObject>();
    
    private Vector3 centerPosition; // Vị trí trung tâm orbit ban đầu
    private TrailRenderer trailRenderer;
    private float currentY; // Y position hiện tại để smooth
    private Collider2D trailCollider; // Để detect collision
    
    public enum TrailType
    {
        Fire,    // Lửa - nổ đen
        Ice,     // Băng - hoá băng  
        Nature,  // Lá - hoá lá
        Dark     // Dark magic - hố đen
    }

    void Start()
    {
        // Lưu vị trí ban đầu làm center (chỉ X và Z)
        centerPosition = transform.localPosition;
        currentY = centerPosition.y; // Khởi tạo current Y
        
        trailRenderer = GetComponent<TrailRenderer>();
        trailCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        // // Tính orbit radius dựa trên dragons
        // float dynamicRadius = CalculateDynamicRadius();
        
        // // Tính center position động dựa trên dragons
        // Vector3 dynamicCenter = CalculateDynamicCenter();
        
        // // Tính Y position dựa trên dragons với damping
        // float targetY = CalculateDragonY();
        // currentY = Mathf.Lerp(currentY, targetY, yDampingSpeed * Time.deltaTime);
        
        // // Object di chuyển theo hình tròn với depth effect
        // // Orbit speed thay đổi theo scrollBackSpeed
        // float currentOrbitSpeed = orbitSpeed * GameManager.Instance.ScrollBackSpeed;
        // float angle = Time.time * currentOrbitSpeed * Mathf.Deg2Rad;
        
        // Vector3 orbitOffset = new Vector3(
        //     Mathf.Cos(angle) * dynamicRadius,
        //     0, // Y sẽ được set riêng
        //     Mathf.Sin(angle) * dynamicRadius * 0.5f // Z depth - nhỏ hơn X để tạo ellipse
        // );
        
        // Vector3 newPosition = dynamicCenter + orbitOffset;
        // newPosition.y = currentY + 0.7f; // Dùng smooth Y thay vì direct Y
        
        // // Adjust Z để có depth relative to dragon
        // if (GameManager.Instance.Zombies.FirstDragon != null)
        // {
        //     float dragonZ = GameManager.Instance.Zombies.FirstDragon.transform.position.z;
        //     newPosition.z = dragonZ + orbitOffset.z; // Trail ở trước/sau dragon
            
        //     // Điều chỉnh sorting order dựa trên Z position
        //     UpdateSortingOrder(orbitOffset.z);
        // }
        
        // transform.localPosition = newPosition;
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
        
        // Option 1: Dựa trên số lượng dragons
        float countBasedRadius = baseOrbitRadius + (dragonManager.Count * radiusMultiplier);
        
        return countBasedRadius;
    }
    
    private Vector3 CalculateDynamicCenter()
    {
        DragonManager dragonManager = GameManager.Instance.Zombies;
        
        if (dragonManager == null || dragonManager.FirstDragon == null)
            return centerPosition; // Fallback về center ban đầu
        
        // Lấy vị trí FirstDragon
        Vector3 firstDragonPos = dragonManager.FirstDragon.transform.position;
        
        // Tính toán offset dựa trên số lượng dragons
        float baseOffset = 0.1f; // Khoảng cách cố định
        float countOffset = dragonManager.Count * 0.2f; // Offset dựa trên số lượng
        float totalOffset = baseOffset + countOffset;
        
        // Center position = FirstDragon position - offset
        Vector3 dynamicCenter = new Vector3(
            firstDragonPos.x - totalOffset,
            centerPosition.y, // Giữ Y gốc cho center, Y thực sẽ được tính riêng
            firstDragonPos.z
        );
        
        return dynamicCenter;
    }
    
    private float CalculateDragonY()
    {
        DragonManager dragonManager = GameManager.Instance.Zombies;
        
        if (dragonManager == null || dragonManager.FirstDragon == null)
            return centerPosition.y; // Fallback về Y ban đầu
        
        // Sync với Y của dragon đầu tiên
        return dragonManager.FirstDragon.transform.position.y;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 10% chance phá vỡ vật thể
        if (Random.Range(0f, 1f) <= destroyChance)
        {
            ApplyTrailEffect(other.gameObject);
        }
    }
    
    private void ApplyTrailEffect(GameObject target)
    {
        // Null check đầu tiên
        if (target == null) return;
        
        // Kiểm tra có thể phá vỡ không (tương tự hoá vàng logic)
        if (!CanDestroyObject(target)) return;
        
        // Check nếu object đã bị marked for destruction
        if (!target.activeInHierarchy) return;
        
        switch (trailType)
        {
            case TrailType.Fire:
                ApplyEffectToTarget(target, fireSprite);
                break;
            case TrailType.Ice:
                ApplyEffectToTarget(target, iceSprite);
                break;
            case TrailType.Nature:
                ApplyEffectToTarget(target, leafSprite);
                break;
            case TrailType.Dark:
                ApplyEffectToTarget(target, darkHoleSprite);
                break;
        }
    }
    
    private bool CanDestroyObject(GameObject target)
    {
        // Tương tự logic hoá vàng - chỉ phá được certain objects
        string tag = target.tag;
        return tag == "Object" || tag == "Bomb" || tag == "Box" || tag == "Obstacle";
    }
    
    private void ApplyEffectToTarget(GameObject target, Sprite effectSprite)
    {
        if (target == null) return;
        
        // Tạo effect sprite tại vị trí của object
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
        
        // Return to pool ngay lập tức (object biến mất, thay bằng effect sprite)
        ReturnToPool(target);
    }

    private void ReturnToPool(GameObject target)
    {
        if (target == null) return;
        
        // Special handling for Box - generate prey before returning to pool
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
        
        PoolableObject poolable = target.GetComponent<PoolableObject>();
        if (poolable != null)
        {
            // TRỰC TIẾP gọi RemoveSelf() thay vì move off-screen để tránh race condition
            // Sử dụng reflection để gọi protected method RemoveSelf()
            var removeSelfMethod = poolable.GetType().GetMethod("RemoveSelf", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            removeSelfMethod?.Invoke(poolable, null);
        }
        else
        {
            // Nếu không có poolable, chỉ disable thay vì destroy
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
        
        // Assign effect sprite nếu có
        if (effectSprite != null)
        {
            effectRenderer.sprite = effectSprite;
        }
        
        // Set sorting layer để hiển thị đúng
        effectRenderer.sortingLayerName = "UI";
        effectRenderer.sortingOrder = 10;
        
        // Thêm animation bay lên và biến mất
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
                float fadeProgress = (progress - 0.5f) * 2f; // 0 to 1 trong nửa cuối
                Color color = renderer.color;
                color.a = 1f - fadeProgress;
                renderer.color = color;
            }
            
            yield return null;
        }
        
        // Destroy leaf object khi animation hoàn thành
        if (leafObject != null)
        {
            Destroy(leafObject);
        }
    }
    
    private GameObject CreateEffectSprite(GameObject target, Sprite effectSprite)
    {
        if (target == null || effectSprite == null) return null;
        
        // Tạo GameObject mới cho effect sprite
        GameObject effectObject = new GameObject($"EffectSprite_{target.name}");
        effectObject.transform.position = target.transform.position;
        effectObject.transform.SetParent(target.transform); // Gắn làm child của target
        
        // Thêm SpriteRenderer component
        SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        effectRenderer.sprite = effectSprite;
        
        // Set sorting layer để hiển thị trên object gốc
        SpriteRenderer targetRenderer = target.GetComponentInChildren<SpriteRenderer>();
        if (targetRenderer != null)
        {
            effectRenderer.sortingLayerName = targetRenderer.sortingLayerName;
            effectRenderer.sortingOrder = targetRenderer.sortingOrder + 1; // Hiển thị trên object gốc
        }
        else
        {
            effectRenderer.sortingLayerName = "UI";
            effectRenderer.sortingOrder = 5;
        }
        
        // Lưu vào dictionary để cleanup sau
        effectSprites[target] = effectObject;
        
        return effectObject;
    }
    
    private void RemoveEffectSprite(GameObject target)
    {
        if (target != null && effectSprites.ContainsKey(target))
        {
            GameObject effectSprite = effectSprites[target];
            if (effectSprite != null)
            {
                Destroy(effectSprite);
            }
            effectSprites.Remove(target);
        }
    }
    
}