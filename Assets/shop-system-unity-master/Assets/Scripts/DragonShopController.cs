// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class DragonShopController : MonoBehaviour
// {
//     [Header("Dragons")]
//     public GameObject[] dragons;
//     private GameObject selectedDragon;
    
//     private ShopController shopController;

//     private void Start()
//     {
//         shopController = GetComponent<ShopController>();
        
//         if (dragons != null && dragons.Length > 0)
//         {
//             LoadProgress();
//             LoadSelectedDragon();
//         }
//     }

//     public void OnClick(int index)
//     {
//         if (dragons != null && index >= 0 && index < dragons.Length)
//         {
//             SelectDragon(dragons[index]);
//         }
//         else
//         {
//             Debug.LogError($"Dragon index {index} is out of bounds! Array length: {(dragons != null ? dragons.Length : 0)}");
//         }
//     }

//     public bool TryUnlock()
//     {
//         if (selectedDragon == null) return false;
        
//         int index = GetDragonIndex(selectedDragon);
//         int cost = GetDragonCost(selectedDragon);
        
//         if (shopController.TryPurchase(cost))
//         {
//             // Unlock successful
//             GetChildGameObject(selectedDragon, 3).SetActive(false);

//             // Unlock dragon and automatically add to spawn pool
//             PlayerPrefs.SetInt("dragonStatus" + index, 1);
//             PlayerPrefs.SetInt("dragonInPool" + index, 1);
            
//             // Show selection indicator
//             GetChildGameObject(selectedDragon, 1).SetActive(true);
            
//             PlayerPrefs.Save();
            
//             Debug.Log($"Unlocked Dragon {index} and added to spawn pool!");
//             return true;
//         }
        
//         return false;
//     }

//     private void SelectDragon(GameObject obj)
//     {
//         if (obj == null) return;
        
//         int index = GetDragonIndex(obj);
        
//         // Clear any other controller's selection
//         shopController.ClearAllSelections();
        
//         // Check if dragon is unlocked
//         if (GetChildGameObject(obj, 3) != null && GetChildGameObject(obj, 3).activeSelf == true)
//         {
//             // Dragon is locked, show purchase UI
//             if (selectedDragon != null)
//                 GetChildGameObject(selectedDragon, 1).SetActive(false);
            
//             selectedDragon = obj;
//             GetChildGameObject(obj, 1).SetActive(true);
            
//             // Update shop UI
//             int cost = GetDragonCost(obj);
//             shopController.SetPurchaseUI(cost, $"Dragon {index}");
//         }
//         else
//         {
//             // Dragon is unlocked, toggle selection for pool
//             bool currentlyEnabled = PlayerPrefs.GetInt("dragonInPool" + index, index == 0 ? 1 : 0) == 1;
            
//             if (currentlyEnabled)
//             {
//                 // Remove from pool
//                 PlayerPrefs.SetInt("dragonInPool" + index, 0);
//                 GetChildGameObject(obj, 1).SetActive(false);
//                 Debug.Log($"Removed Dragon {index} from spawn pool");
//             }
//             else
//             {
//                 // Add to pool
//                 PlayerPrefs.SetInt("dragonInPool" + index, 1);
//                 GetChildGameObject(obj, 1).SetActive(true);
//                 Debug.Log($"Added Dragon {index} to spawn pool");
//             }
            
//             selectedDragon = null;
//             shopController.ClearPurchaseUI();
//             PlayerPrefs.Save();
//         }
//     }

//     private void LoadProgress()
//     {
//         for (int i = 0; i < dragons.Length; i++)
//         {
//             // Load unlock status
//             if (PlayerPrefs.GetInt("dragonStatus" + i) == 1)
//             {
//                 GetChildGameObject(dragons[i], 3).SetActive(false);
//             }
            
//             // Load selection status for spawn pool (only if unlocked)
//             if (PlayerPrefs.GetInt("dragonStatus" + i) == 1)
//             {
//                 bool inPool = PlayerPrefs.GetInt("dragonInPool" + i, i == 0 ? 1 : 0) == 1;
//                 GetChildGameObject(dragons[i], 1).SetActive(inPool);
//             }
//         }
        
//         // Dragon 0 should always be unlocked and in pool (default)
//         if (dragons.Length > 0)
//         {
//             GetChildGameObject(dragons[0], 3).SetActive(false);
//             PlayerPrefs.SetInt("dragonStatus0", 1);
//             PlayerPrefs.SetInt("dragonInPool0", 1);
//             GetChildGameObject(dragons[0], 1).SetActive(true);
//         }
//     }

//     private void LoadSelectedDragon()
//     {
//         if (PlayerPrefs.HasKey("selectedDragon"))
//         {
//             int savedIndex = PlayerPrefs.GetInt("selectedDragon");
//             if (savedIndex < dragons.Length)
//             {
//                 SelectDragon(dragons[savedIndex]);
//             }
//         }
//     }

//     private int GetDragonCost(GameObject dragonObj)
//     {
//         GameObject priceText = GetChildGameObject(dragonObj, 0);
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
//             Debug.LogError($"Cannot get child {child} from dragon object {(obj != null ? obj.name : "null")}");
//             return null;
//         }
//         return obj.transform.GetChild(child).gameObject;
//     }

//     private int GetDragonIndex(GameObject obj)
//     {
//         for (int i = 0; i < dragons.Length; i++)
//         {
//             if (dragons[i] == obj)
//                 return i;
//         }
//         return -1;
//     }

//     public void ClearSelection()
//     {
//         if (selectedDragon != null)
//         {
//             GetChildGameObject(selectedDragon, 1).SetActive(false);
//             selectedDragon = null;
//         }
//     }

//     public bool HasSelection()
//     {
//         return selectedDragon != null;
//     }

//     public GameObject GetSelectedDragon()
//     {
//         return selectedDragon;
//     }

//     private void OnScreenLeave()
//     {
//         if (selectedDragon != null)
//         {
//             int index = GetDragonIndex(selectedDragon);
//             PlayerPrefs.SetInt("selectedDragon", index);
//             PlayerPrefs.SetInt("dragonStatus" + index, 1);
//         }
//     }
// }