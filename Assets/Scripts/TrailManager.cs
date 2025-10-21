using System.Collections;
using System.Collections.Generic;
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
        
        // Get components với null check
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            Debug.LogWarning($"TrailManager on {gameObject.name}: TrailRenderer component not found!");
        }
        
        trailCollider = GetComponent<Collider2D>();
        
        // Nếu chưa có collider, tạo circle collider
        if (trailCollider == null)
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = effectRadius;
            circle.isTrigger = true;
            trailCollider = circle;
        }
    }

    void Update()
    {
        // Check null trước khi làm gì
        if (this == null || transform == null) return;
        
        // Tính orbit radius dựa trên dragons
        float dynamicRadius = CalculateDynamicRadius();
        
        // Tính center position động dựa trên dragons
        Vector3 dynamicCenter = CalculateDynamicCenter();
        
        // Tính Y position dựa trên dragons với damping
        float targetY = CalculateDragonY();
        currentY = Mathf.Lerp(currentY, targetY, yDampingSpeed * Time.deltaTime);
        
        // Object di chuyển theo hình tròn với depth effect
        // Orbit speed thay đổi theo scrollBackSpeed
        float currentOrbitSpeed = orbitSpeed * GameManager.Instance.ScrollBackSpeed;
        float angle = Time.time * currentOrbitSpeed * Mathf.Deg2Rad;
        
        Vector3 orbitOffset = new Vector3(
            Mathf.Cos(angle) * dynamicRadius,
            0, // Y sẽ được set riêng
            Mathf.Sin(angle) * dynamicRadius * 0.5f // Z depth - nhỏ hơn X để tạo ellipse
        );
        
        Vector3 newPosition = dynamicCenter + orbitOffset;
        newPosition.y = currentY + 0.3f; // Dùng smooth Y thay vì direct Y
        
        // Adjust Z để có depth relative to dragon
        if (GameManager.Instance.Zombies.FirstDragon != null)
        {
            float dragonZ = GameManager.Instance.Zombies.FirstDragon.transform.position.z;
            newPosition.z = dragonZ + orbitOffset.z; // Trail ở trước/sau dragon
            
            // Điều chỉnh sorting order dựa trên Z position
            UpdateSortingOrder(orbitOffset.z);
        }
        
        transform.localPosition = newPosition;
    }
    
    private void UpdateSortingOrder(float zOffset)
    {
        // Double check null cho trailRenderer
        if (trailRenderer == null || !trailRenderer) return;
        
        // Khi trail ở phía trước (Z > 0) → UI layer (trước dragon)
        // Khi trail ở phía sau (Z < 0) → Vehicle layer (sau dragon)
        if (zOffset > 0)
        {
            // Trail ở phía trước
            trailRenderer.sortingLayerName = "UI"; // Trước dragon
        }
        else
        {
            // Trail ở phía sau  
            trailRenderer.sortingLayerName = "Vehicle"; // Sau dragon
        }
    }
    
    private float CalculateDynamicRadius()
    {
        DragonManager dragonManager = GameManager.Instance.Zombies;
        
        if (dragonManager == null || dragonManager.Count == 0)
            return baseOrbitRadius;
        
        // Option 1: Dựa trên số lượng dragons
        float countBasedRadius = baseOrbitRadius + (dragonManager.Count * radiusMultiplier);
        
        // Option 2: Dựa trên khoảng cách giữa dragon đầu và cuối (nếu có nhiều dragon)
        // if (dragonManager.Count > 1)
        // {
        //     // DragonManager không có LastDragon property
        //     // Cần implement logic tìm dragon cuối cùng
        //     return baseOrbitRadius + (distance * radiusMultiplier);
        // }
        
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
        if (target == null || this == null || gameObject == null) return;
        
        // Kiểm tra có thể phá vỡ không (tương tự hoá vàng logic)
        if (!CanDestroyObject(target)) return;
        
        // Check nếu object đã bị marked for destruction
        if (!target.activeInHierarchy) return;
        
        switch (trailType)
        {
            case TrailType.Fire:
                ApplyFireEffect(target);
                break;
            case TrailType.Ice:
                ApplyIceEffect(target);
                break;
            case TrailType.Nature:
                ApplyNatureEffect(target);
                break;
            case TrailType.Dark:
                ApplyDarkEffect(target);
                break;
        }
    }
    
    private bool CanDestroyObject(GameObject target)
    {
        // Tương tự logic hoá vàng - chỉ phá được certain objects
        string tag = target.tag;
        return tag == "Object" || tag == "Bomb" || tag == "Box" || tag == "Obstacle";
    }
    
    private void ApplyFireEffect(GameObject target)
    {
        if (target == null || this == null || gameObject == null) return;
        
        // Lửa - nổ đen vật thể
        Debug.Log($"Fire Trail Effect: Exploding {target.name}");
        
        // Tạo explosion effect
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CallExplosion(true, target.transform.position);
        }
        
        // Generate preys if it's a Box
        Box box = target.GetComponent<Box>();
        if (box != null)
        {
            // Call GeneratePreys method from Box
            box.GetType().GetMethod("GeneratePreys", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(box, null);
        }
        
        // Return to pool thay vì Destroy để tránh conflict
        PoolableObject poolable = target.GetComponent<PoolableObject>();
        if (poolable != null)
        {
            // Tìm manager tương ứng dựa trên tag
            Manager targetManager = FindManagerForObject(target);
            if (targetManager != null)
            {
                targetManager.ReturnItem(poolable);
            }
            else
            {
                Destroy(target);
            }
        }
        else
        {
            Destroy(target);
        }
    }
    
    private void ApplyIceEffect(GameObject target)
    {
        if (target == null || this == null || gameObject == null) return;
        
        // Băng - hoá băng vật thể (change color/material to ice)
        Debug.Log($"Ice Trail Effect: Freezing {target.name}");
        
        // Change to ice color/material
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.cyan; // Màu băng
        }
        
        // Optional: Freeze movement if it has Rigidbody
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        // Generate preys if it's a Box
        Box box = target.GetComponent<Box>();
        if (box != null)
        {
            // Call GeneratePreys method from Box
            box.GetType().GetMethod("GeneratePreys", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(box, null);
        }
        
        // Return to pool immediately - no delay
        PoolableObject poolable = target.GetComponent<PoolableObject>();
        if (poolable != null)
        {
            Manager targetManager = FindManagerForObject(target);
            if (targetManager != null)
            {
                targetManager.ReturnItem(poolable);
            }
            else
            {
                Destroy(target);
            }
        }
        else
        {
            Destroy(target);
        }
    }
    
    private void ApplyNatureEffect(GameObject target)
    {
        if (target == null || this == null || gameObject == null) return;
        
        // Lá - hoá lá vật thể (change color to green/leaf)
        Debug.Log($"Nature Trail Effect: Converting {target.name} to nature");
        
        // Change to nature color
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.green; // Màu lá
        }
        
        // Optional: Add leaf particle effect
        
        // Generate preys if it's a Box
        Box box = target.GetComponent<Box>();
        if (box != null)
        {
            // Call GeneratePreys method from Box
            box.GetType().GetMethod("GeneratePreys", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(box, null);
        }
        
        // Return to pool immediately - no delay
        PoolableObject poolable = target.GetComponent<PoolableObject>();
        if (poolable != null)
        {
            Manager targetManager = FindManagerForObject(target);
            if (targetManager != null)
            {
                targetManager.ReturnItem(poolable);
            }
            else
            {
                Destroy(target);
            }
        }
        else
        {
            Destroy(target);
        }
    }
    
    private void ApplyDarkEffect(GameObject target)
    {
        if (target == null || this == null || gameObject == null) return;
        
        // Dark magic - hoá hố đen (create black hole effect)
        Debug.Log($"Dark Trail Effect: Creating black hole at {target.name}");
        
        // Change to dark color
        SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.black; // Màu đen
        }
        
        // Create shrinking effect (hố đen)
        StartCoroutine(ShrinkToBlackHole(target));
    }
    
    private System.Collections.IEnumerator ShrinkToBlackHole(GameObject target)
    {
        if (target == null || target.transform == null) yield break; // Exit nếu object đã bị destroy
        
        Vector3 originalScale = target.transform.localScale;
        float duration = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < duration && target != null && target.transform != null) // Check null trong loop
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Shrink object - check null trước khi access
            target.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
            
            yield return null;
        }
        
        // Return to pool when fully shrunk - check null cuối cùng
        if (target != null)
        {
            // Generate preys if it's a Box
            Box box = target.GetComponent<Box>();
            if (box != null)
            {
                // Call GeneratePreys method from Box
                box.GetType().GetMethod("GeneratePreys", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(box, null);
            }
            
            PoolableObject poolable = target.GetComponent<PoolableObject>();
            if (poolable != null)
            {
                Manager targetManager = FindManagerForObject(target);
                if (targetManager != null)
                {
                    targetManager.ReturnItem(poolable);
                }
                else
                {
                    Destroy(target);
                }
            }
            else
            {
                Destroy(target);
            }
        }
    }
    

    
    private Manager FindManagerForObject(GameObject target)
    {
        if (target == null || this == null || gameObject == null) return null;
        
        string tag = target.tag;
        
        // Tìm manager tương ứng
        switch (tag)
        {
            case "Bomb":
                return FindObjectOfType<BombManager>();
            case "Object":
            case "Box":
                return FindObjectOfType<BoxManager>();
            default:
                return null;
        }
    }
}
