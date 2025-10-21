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
        newPosition.y = currentY + 0.7f; // Dùng smooth Y thay vì direct Y
        
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
        if (target == null) return;
        
        // Lửa - nổ đen vật thể
        Debug.Log($"Fire Trail Effect: Exploding {target.name}");
        
        // Thêm fire sprite overlay
        if (fireSprite != null)
        {
            CreateEffectSprite(target, fireSprite);
        }
        
        // Tạo explosion effect
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CallExplosion(true, target.transform.position);
        }
        
        // Return to pool sau khi apply effect với delay để visual effect hoàn thành
        StartCoroutine(ReturnToPoolAfterDelay(target, 0.5f));
    }
    
    private System.Collections.IEnumerator ReturnToPoolAfterDelay(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (target != null && target.activeInHierarchy)
        {
            // Reset object properties trước khi return về pool
            ResetObjectProperties(target);
            
            // Use ReturnToPool helper method
            ReturnToPool(target);
        }
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
            // Trigger the object's own removal logic
            poolable.transform.position = new Vector3(-1000, -1000, 0); // Move off-screen
            // This will trigger DestroyOnOutOfBounds -> RemoveSelf on next Update
        }
        else
        {
            // Nếu không có poolable, chỉ disable thay vì destroy
            target.SetActive(false);
        }
    }
    
    private void ResetObjectProperties(GameObject target)
    {
        if (target == null) return;
        
        // Remove effect sprite overlay trước tiên
        RemoveEffectSprite(target);
        
        // Reset special properties cho từng loại object
        if (target.CompareTag("Box"))
        {
            // Reset Box về trạng thái ban đầu
            Box box = target.GetComponent<Box>();
            if (box != null && originalBoxDragonNeeded.ContainsKey(target))
            {
                // Restore giá trị gốc đã lưu
                var numberDragonNeededField = box.GetType().GetField("numberDragonNeeded", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (numberDragonNeededField != null)
                {
                    int originalValue = originalBoxDragonNeeded[target];
                    numberDragonNeededField.SetValue(box, originalValue);
                    originalBoxDragonNeeded.Remove(target); // Cleanup
                    Debug.Log($"Reset: Box numberDragonNeeded restored to {originalValue}");
                }
            }
        }
        else if (target.CompareTag("Bomb"))
        {
            // Reset Bomb collision về trạng thái gốc
            Bomb bomb = target.GetComponent<Bomb>();
            if (bomb != null && originalBombCollisionEnabled.ContainsKey(target))
            {
                Collider2D bombCollider = bomb.GetComponent<Collider2D>();
                if (bombCollider != null)
                {
                    bool originalEnabled = originalBombCollisionEnabled[target];
                    bombCollider.enabled = originalEnabled;
                    originalBombCollisionEnabled.Remove(target); // Cleanup
                }
            }
        }
        
        // Reset rigidbody properties
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector2.zero;
        }
        
        // Reset scale về original
        target.transform.localScale = Vector3.one;
    }
    
    private void ApplyIceEffect(GameObject target)
    {
        if (target == null) return;
        
        // Thêm ice sprite overlay
        if (iceSprite != null)
        {
            CreateEffectSprite(target, iceSprite);
        }
        
        // Special effects cho từng loại object
        if (target.CompareTag("Box"))
        {
            // Box: giảm numberDragonNeeded thành 1
            Box box = target.GetComponent<Box>();
            if (box != null)
            {
                // Lưu giá trị gốc trước khi modify
                var numberDragonNeededField = box.GetType().GetField("numberDragonNeeded", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (numberDragonNeededField != null)
                {
                    int originalValue = (int)numberDragonNeededField.GetValue(box);
                    originalBoxDragonNeeded[target] = originalValue;
                    
                    numberDragonNeededField.SetValue(box, 1);
                }
            }
        }
        else if (target.CompareTag("Bomb"))
        {
            Bomb bomb = target.GetComponent<Bomb>();
            if (bomb != null)
            {
                Collider2D bombCollider = bomb.GetComponent<Collider2D>();
                if (bombCollider != null)
                {
                    // Lưu giá trị gốc trước khi modify
                    originalBombCollisionEnabled[target] = bombCollider.enabled;
                    
                    bombCollider.enabled = false;
                }
            }
        }
        
        // Optional: Freeze movement if it has Rigidbody
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        // Sau 0.5s tự động restore và biến mất
        StartCoroutine(IceEffectDuration(target, 0.5f));
    }
    
    private System.Collections.IEnumerator IceEffectDuration(GameObject target, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        if (target != null && target.activeInHierarchy)
        {
            // Reset object properties về trạng thái ban đầu
            ResetObjectProperties(target);
            
            // Return object to pool (biến mất)
            ReturnToPool(target);
        }
    }
    
    private void ApplyNatureEffect(GameObject target)
    {
        if (target == null) return;
        
        // Tạo leaf sprite tại vị trí của object
        CreateLeafSprite(target.transform.position);
        
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
        
        // Return to pool ngay lập tức (object biến mất, thay bằng leaf sprite)
        ReturnToPool(target);
    }
    
    private void CreateLeafSprite(Vector3 position)
    {
        // Tạo GameObject mới cho leaf sprite
        GameObject leafObject = new GameObject("LeafSprite");
        leafObject.transform.position = position;
        
        // Thêm SpriteRenderer component
        SpriteRenderer leafRenderer = leafObject.AddComponent<SpriteRenderer>();
        
        // Assign leaf sprite nếu có
        if (leafSprite != null)
        {
            leafRenderer.sprite = leafSprite;
        }
        
        // Set sorting layer để hiển thị đúng
        leafRenderer.sortingLayerName = "UI";
        leafRenderer.sortingOrder = 10;
        
        // Thêm animation bay lên và biến mất
        StartCoroutine(LeafFallAnimation(leafObject));
    }
    
    private System.Collections.IEnumerator LeafFallAnimation(GameObject leafObject)
    {
        if (leafObject == null) yield break;
        
        Vector3 startPosition = leafObject.transform.position;
        Vector3 endPosition = startPosition + Vector3.up * 2f; // bay lên 2 đơn vị
        SpriteRenderer renderer = leafObject.GetComponent<SpriteRenderer>();
        
        float duration = 2f;
        float elapsed = 0f;
        
        while (elapsed < duration && leafObject != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Animation bay lên
            leafObject.transform.position = Vector3.Lerp(startPosition, endPosition, progress);
            
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
    
    private void ApplyDarkEffect(GameObject target)
    {
        // Dark magic - hoá hố đen (create black hole effect)
        Debug.Log($"Dark Trail Effect: Creating black hole at {target.name}");
        
        // Thêm dark hole sprite overlay
        if (darkHoleSprite != null)
        {
            CreateEffectSprite(target, darkHoleSprite);
        }
        
        // Tắt collision tương tự Ice effect
        if (target.CompareTag("Bomb"))
        {
            Bomb bomb = target.GetComponent<Bomb>();
            if (bomb != null)
            {
                Collider2D bombCollider = bomb.GetComponent<Collider2D>();
                if (bombCollider != null)
                {
                    // Lưu giá trị gốc trước khi modify
                    if (!originalBombCollisionEnabled.ContainsKey(target))
                    {
                        originalBombCollisionEnabled[target] = bombCollider.enabled;
                    }
                    bombCollider.enabled = false;
                }
            }
        }
        
        // Tắt movement
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        // Create shrinking effect (hố đen) - chỉ shrink object gốc
        StartCoroutine(ShrinkToBlackHole(target));
    }
    
    private System.Collections.IEnumerator ShrinkToBlackHole(GameObject target)
    {
        if (target == null || target.transform == null) yield break; // Exit nếu object đã bị destroy
        
        // Tìm sprite renderer gốc của object (không phải effect sprite)
        SpriteRenderer targetRenderer = null;
        foreach (Transform child in target.transform)
        {
            if (!child.name.StartsWith("EffectSprite_"))
            {
                SpriteRenderer childRenderer = child.GetComponent<SpriteRenderer>();
                if (childRenderer != null)
                {
                    targetRenderer = childRenderer;
                    break;
                }
            }
        }
        
        // Nếu không tìm thấy child renderer, thử lấy renderer trực tiếp từ target
        if (targetRenderer == null)
        {
            targetRenderer = target.GetComponent<SpriteRenderer>();
        }
        
        if (targetRenderer == null) yield break; // Không có sprite renderer để shrink
        
        Vector3 originalScale = targetRenderer.transform.localScale;
        float duration = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration && target != null && targetRenderer != null) // Check null trong loop
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            // Chỉ shrink sprite renderer gốc, không shrink dark hole sprite overlay
            targetRenderer.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
            
            yield return null;
        }
        
        // Return to pool when fully shrunk - check null cuối cùng
        if (target != null)
        {
            // Reset properties trước khi return về pool
            ResetObjectProperties(target);
            
            // Use ReturnToPool helper method
            ReturnToPool(target);
        }
    }
}