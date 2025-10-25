using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paralax : MonoBehaviour
{
    private float startPos;
    private float startPosY; 

    public float parallaxEffect = 0.5f; 
    [SerializeField] private bool moveRight = false; 
    private float sharedLength = 20f; 
    
    [Header("Floating Animation")]
    [SerializeField] private bool enableFloating = false; 
    [SerializeField] private float floatingAmplitude = 0.5f; 
    [SerializeField] private float floatingSpeed = 1f; 
    [SerializeField] private float floatingOffset = 0f;
    

    void Start()
    {
        startPos = transform.position.x;
        startPosY = transform.position.y; 
        sharedLength = GameManager.ScreenWidth;
    }

    void Update() 
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
        
        if (moveRight)
        {
            if (transform.position.x >= startPos + sharedLength)
            {
                transform.position = new Vector3(transform.position.x - sharedLength, transform.position.y, transform.position.z);
            }
        }
        else
        {
            if (transform.position.x <= startPos - sharedLength)
            {
                transform.position = new Vector3(transform.position.x + sharedLength, transform.position.y, transform.position.z);
            }
        }
    }
}