using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paralax : MonoBehaviour
{
    private float startPos;
    private float startPosY; // Y position gốc cho floating animation

    public float parallaxEffect = 0.5f; 
    [SerializeField] private bool moveRight = false; // true = bay qua phải, false = scroll left như bình thường
    private float sharedLength = 20f; // Length chung cho tất cả backgrounds
    
    [Header("Floating Animation")]
    [SerializeField] private bool enableFloating = false; // Bật/tắt floating animation
    [SerializeField] private float floatingAmplitude = 0.5f; // Biên độ dao động (units)
    [SerializeField] private float floatingSpeed = 1f; // Tốc độ dao động
    [SerializeField] private float floatingOffset = 0f; // Phase offset để tránh đồng bộ
    

    void Start()
    {
        startPos = transform.position.x;
        startPosY = transform.position.y; // Lưu Y position gốc
        sharedLength = GameManager.ScreenWidth;
    }

    void Update() // Sử dụng FixedUpdate thay vì Update để mượt hơn
    {

        float moveSpeed = (GameManager.Instance.ScrollBackSpeed/15f) * parallaxEffect * Time.fixedDeltaTime;
        
        // Smooth movement based on direction
        Vector3 moveDirection = moveRight ? Vector3.right : Vector3.left;
        Vector3 newPosition = transform.position + moveDirection * moveSpeed;
        
        // Apply floating animation if enabled
        if (enableFloating)
        {
            float floatingY = startPosY + Mathf.Sin((Time.time + floatingOffset) * floatingSpeed) * floatingAmplitude;
            newPosition.y = floatingY;
        }
        
        transform.position = newPosition;
        
        // Infinite scrolling logic based on direction
        if (moveRight)
        {
            // Moving right - reset when goes too far right
            if (transform.position.x >= startPos + sharedLength)
            {
                transform.position = new Vector3(transform.position.x - sharedLength, transform.position.y, transform.position.z);
            }
        }
        else
        {
            // Moving left - reset when goes too far left
            if (transform.position.x <= startPos - sharedLength)
            {
                transform.position = new Vector3(transform.position.x + sharedLength, transform.position.y, transform.position.z);
            }
        }
    }
}