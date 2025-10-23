// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class PowerUpShopController : MonoBehaviour
// {
//     [Header("PowerUps")]
//     public GameObject[] powerUps;
//     private GameObject selectedPowerUp;
    
//     private ShopController shopController;

//     private void Start()
//     {
//         shopController = GetComponent<ShopController>();
        
//         if (powerUps != null && powerUps.Length > 0)
//         {
//             LoadProgress();
//         }
//     }

//     public void OnClick(int index)
//     {
//         if (powerUps != null && index >= 0 && index < powerUps.Length)
//         {
//             SelectPowerUp(powerUps[index]);
//         }
//         else
//         {
//             Debug.LogError($"PowerUp index {index} is out of bounds! Array length: {(powerUps != null ? powerUps.Length : 0)}");
//         }
//     }

//     public bool TryUnlock()
//     {
//         if (selectedPowerUp == null) return false;
        
//         int index = GetPowerUpIndex(selectedPowerUp);
//         int cost = GetPowerUpCost(selectedPowerUp);
        
//         if (shopController.TryPurchase(cost))
//         {
//             // Unlock successful
//             GetChildGameObject(selectedPowerUp, 3).SetActive(false);

//             // Unlock powerup
//             PlayerPrefs.SetInt("powerUpStatus" + index, 1);
            
//             // Show unlocked indicator
//             GetChildGameObject(selectedPowerUp, 1).SetActive(true);
            
//             PlayerPrefs.Save();
            
//             Debug.Log($"Unlocked PowerUp {index}!");
//             return true;
//         }
        
//         return false;
//     }

//     private void SelectPowerUp(GameObject obj)
//     {
//         if (obj == null) return;
        
//         int index = GetPowerUpIndex(obj);
        
//         // Clear any other controller's selection
//         shopController.ClearAllSelections();
        
//         // Check if powerup is unlocked
//         if (GetChildGameObject(obj, 3) != null && GetChildGameObject(obj, 3).activeSelf == true)
//         {
//             // PowerUp is locked, show purchase UI
//             if (selectedPowerUp != null)
//                 GetChildGameObject(selectedPowerUp, 1).SetActive(false);
            
//             selectedPowerUp = obj;
//             GetChildGameObject(obj, 1).SetActive(true);
            
//             // Update shop UI
//             int cost = GetPowerUpCost(obj);
//             shopController.SetPurchaseUI(cost, $"PowerUp {index}");
//         }
//         else
//         {
//             // PowerUp is unlocked, just show it's selected temporarily
//             selectedPowerUp = null;
//             shopController.ClearPurchaseUI();
            
//             Debug.Log($"PowerUp {index} is already unlocked!");
//         }
//     }

//     private void LoadProgress()
//     {
//         for (int i = 0; i < powerUps.Length; i++)
//         {
//             // Load unlock status
//             if (PlayerPrefs.GetInt("powerUpStatus" + i) == 1)
//             {
//                 GetChildGameObject(powerUps[i], 3).SetActive(false);
//                 GetChildGameObject(powerUps[i], 1).SetActive(true); // Show as unlocked
//             }
//         }
        
//         // PowerUp 0 could be unlocked by default if needed
//         // Uncomment if you want first powerup free
//         /*
//         if (powerUps.Length > 0)
//         {
//             GetChildGameObject(powerUps[0], 3).SetActive(false);
//             PlayerPrefs.SetInt("powerUpStatus0", 1);
//             GetChildGameObject(powerUps[0], 1).SetActive(true);
//         }
//         */
//     }

//     private int GetPowerUpCost(GameObject powerUpObj)
//     {
//         GameObject priceText = GetChildGameObject(powerUpObj, 0);
//         if (priceText != null)
//         {
//             Text textComponent = priceText.GetComponent<Text>();
//             if (textComponent != null && int.TryParse(textComponent.text, out int cost))
//             {
//                 return cost;
//             }
//         }
//         return 0;
//     }

//     private GameObject GetChildGameObject(GameObject obj, int child)
//     {
//         if (obj == null || obj.transform.childCount <= child || child < 0)
//         {
//             Debug.LogError($"Cannot get child {child} from powerup object {(obj != null ? obj.name : "null")}");
//             return null;
//         }
//         return obj.transform.GetChild(child).gameObject;
//     }

//     private int GetPowerUpIndex(GameObject obj)
//     {
//         for (int i = 0; i < powerUps.Length; i++)
//         {
//             if (powerUps[i] == obj)
//                 return i;
//         }
//         return -1;
//     }

//     public void ClearSelection()
//     {
//         if (selectedPowerUp != null)
//         {
//             GetChildGameObject(selectedPowerUp, 1).SetActive(false);
//             selectedPowerUp = null;
//         }
//     }

//     public bool HasSelection()
//     {
//         return selectedPowerUp != null;
//     }

//     public GameObject GetSelectedPowerUp()
//     {
//         return selectedPowerUp;
//     }

//     // Method để check xem powerup có unlocked không (có thể dùng từ gameplay)
//     public bool IsPowerUpUnlocked(int index)
//     {
//         return PlayerPrefs.GetInt("powerUpStatus" + index, 0) == 1;
//     }

//     // Method để get tất cả powerups đã unlock (có thể dùng từ gameplay)
//     public List<int> GetUnlockedPowerUps()
//     {
//         List<int> unlockedPowerUps = new List<int>();
//         for (int i = 0; i < powerUps.Length; i++)
//         {
//             if (IsPowerUpUnlocked(i))
//             {
//                 unlockedPowerUps.Add(i);
//             }
//         }
//         return unlockedPowerUps;
//     }
// }