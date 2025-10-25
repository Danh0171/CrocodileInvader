using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxCopy : MonoBehaviour
{
    [Header("Copy Settings")]
    [SerializeField] private bool isRightCopy = true;  
    [SerializeField] private bool parentMovesRight = false; 
    
    void Start()
    {
        SetupCopyPosition();
    }
    
    void SetupCopyPosition()
    {
        Transform parentSprite = transform.parent;
        
        Vector3 parentPos = parentSprite.position;
        
        // Tính toán vị trí mới dựa trên direction và ScreenWidth
        Vector3 newPos = parentPos;
        
        if (parentMovesRight)
        {
            if (isRightCopy)
                newPos.x = parentPos.x - GameManager.ScreenWidth; 
            else
                newPos.x = parentPos.x + GameManager.ScreenWidth;
        }
        else
        {
            if (isRightCopy)
                newPos.x = parentPos.x + GameManager.ScreenWidth;
            else
                newPos.x = parentPos.x - GameManager.ScreenWidth;
        }
        
        // Áp dụng vị trí mới (world position, không phải local)
        transform.position = newPos;
    }
    
    // Method để re-setup nếu ScreenWidth thay đổi trong runtime
    public void RefreshPosition()
    {
        SetupCopyPosition();
    }
}