using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paralax : MonoBehaviour
{
    private float startPos;
    public float parallaxEffect = 0.5f; // 0 = static, 1 = same speed as scroll
    // [SerializeField] private float baseScrollSpeed = 3f; // Fixed speed thay vì dynamic ScrollBackSpeed
    [SerializeField] private bool moveRight = false; // true = bay qua phải, false = scroll left như bình thường
    private float sharedLength = 20f; // Length chung cho tất cả backgrounds
    

    void Start()
    {
        startPos = transform.position.x;
        sharedLength = GameManager.ScreenWidth;
        Debug.Log($"Parallax initialized - StartPos: {startPos}, Shared Length: {sharedLength}");
    }

    void Update() // Sử dụng FixedUpdate thay vì Update để mượt hơn
    {

        float moveSpeed = (GameManager.Instance.ScrollBackSpeed/15f) * parallaxEffect * Time.fixedDeltaTime;
        
        // Smooth movement based on direction
        Vector3 moveDirection = moveRight ? Vector3.right : Vector3.left;
        Vector3 newPosition = transform.position + moveDirection * moveSpeed;
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