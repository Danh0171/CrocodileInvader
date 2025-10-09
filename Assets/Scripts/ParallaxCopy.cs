using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxCopy : MonoBehaviour
{
    [Header("Copy Settings")]
    [SerializeField] private bool isRightCopy = true;  // true = copy bên phải, false = copy bên trái
    [SerializeField] private bool parentMovesRight = false; // true nếu parent bay qua phải
    
    void Start()
    {
        SetupCopyPosition();
    }
    
    void SetupCopyPosition()
    {
        // Tự động lấy parent làm original sprite
        Transform parentSprite = transform.parent;
        
        // Lấy vị trí của parent (sprite gốc)
        Vector3 parentPos = parentSprite.position;
        
        // Tính toán vị trí mới dựa trên direction và ScreenWidth
        Vector3 newPos = parentPos;
        
        if (parentMovesRight)
        {
            // Parent bay qua phải → copy nên ở bên trái để ready replace
            if (isRightCopy)
                newPos.x = parentPos.x - GameManager.ScreenWidth; // Copy "bên phải" thực ra ở bên trái
            else
                newPos.x = parentPos.x + GameManager.ScreenWidth; // Copy "bên trái" thực ra ở bên phải
        }
        else
        {
            // Parent di chuyển qua trái (normal) → copy ở bên phải
            if (isRightCopy)
                newPos.x = parentPos.x + GameManager.ScreenWidth;
            else
                newPos.x = parentPos.x - GameManager.ScreenWidth;
        }
        
        // Áp dụng vị trí mới (world position, không phải local)
        transform.position = newPos;
        
        Debug.Log($"{gameObject.name} positioned at: {newPos} (Parent: {parentPos}, ScreenWidth: {GameManager.ScreenWidth})");
    }
    
    // Method để re-setup nếu ScreenWidth thay đổi trong runtime
    public void RefreshPosition()
    {
        SetupCopyPosition();
    }
}