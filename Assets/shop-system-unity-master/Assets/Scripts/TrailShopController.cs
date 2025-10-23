// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class TrailShopController : MonoBehaviour
// {
//     [Header("Trails")]
//     public GameObject[] trails;
//     private GameObject selectedTrail;
    
//     private ShopController shopController;

//     private void Start()
//     {
//         shopController = GetComponent<ShopController>();
        
//         if (trails != null && trails.Length > 0)
//         {
//             LoadProgress();
//             LoadSelectedTrail();
//         }
//     }

//     public void OnClick(int index)
//     {
//         if (trails != null && index >= 0 && index < trails.Length)
//         {
//             SelectTrail(trails[index]);
//         }
//         else
//         {
//             Debug.LogError($"Trail index {index} is out of bounds! Array length: {(trails != null ? trails.Length : 0)}");
//         }
//     }

//     public bool TryUnlock()
//     {
//         if (selectedTrail == null) return false;
        
//         int index = GetTrailIndex(selectedTrail);
//         int cost = GetTrailCost(selectedTrail);
        
//         if (shopController.TryPurchase(cost))
//         {
//             // Unlock successful
//             GetChildGameObject(selectedTrail, 3).SetActive(false);

//             // Unlock trail and automatically select it
//             PlayerPrefs.SetInt("trailStatus" + index, 1);
//             PlayerPrefs.SetInt("selectedTrail", index);
            
//             // Show selection indicator
//             GetChildGameObject(selectedTrail, 1).SetActive(true);
            
//             PlayerPrefs.Save();
            
//             Debug.Log($"Unlocked and selected Trail {index}!");
//             return true;
//         }
        
//         return false;
//     }

//     private void SelectTrail(GameObject obj)
//     {
//         if (obj == null) return;
        
//         int index = GetTrailIndex(obj);
        
//         // Clear any other controller's selection
//         shopController.ClearAllSelections();
        
//         // Check if trail is unlocked
//         if (GetChildGameObject(obj, 3) != null && GetChildGameObject(obj, 3).activeSelf == true)
//         {
//             // Trail is locked, show purchase UI
//             if (selectedTrail != null)
//                 GetChildGameObject(selectedTrail, 1).SetActive(false);
            
//             selectedTrail = obj;
//             GetChildGameObject(obj, 1).SetActive(true);
            
//             // Update shop UI
//             int cost = GetTrailCost(obj);
//             shopController.SetPurchaseUI(cost, $"Trail {index}");
//         }
//         else
//         {
//             // Trail is unlocked, select it (only one trail can be selected at a time)
//             int currentSelectedTrail = PlayerPrefs.GetInt("selectedTrail", 0);
            
//             // Deselect previous trail
//             if (currentSelectedTrail != index && currentSelectedTrail < trails.Length)
//             {
//                 GetChildGameObject(trails[currentSelectedTrail], 1).SetActive(false);
//             }
            
//             // Select new trail
//             PlayerPrefs.SetInt("selectedTrail", index);
//             GetChildGameObject(obj, 1).SetActive(true);
//             selectedTrail = null;
//             shopController.ClearPurchaseUI();
            
//             PlayerPrefs.Save();
//             Debug.Log($"Selected Trail {index}!");
//         }
//     }

//     private void LoadProgress()
//     {
//         for (int i = 0; i < trails.Length; i++)
//         {
//             // Load unlock status
//             if (PlayerPrefs.GetInt("trailStatus" + i) == 1)
//             {
//                 GetChildGameObject(trails[i], 3).SetActive(false);
//             }
//         }
        
//         // Trail 0 should always be unlocked (default)
//         if (trails.Length > 0)
//         {
//             GetChildGameObject(trails[0], 3).SetActive(false);
//             PlayerPrefs.SetInt("trailStatus0", 1);
//         }
        
//         // Show selection for currently selected trail
//         int selectedTrailIndex = PlayerPrefs.GetInt("selectedTrail", 0);
//         if (selectedTrailIndex < trails.Length)
//         {
//             GetChildGameObject(trails[selectedTrailIndex], 1).SetActive(true);
//         }
//     }

//     private void LoadSelectedTrail()
//     {
//         if (PlayerPrefs.HasKey("selectedTrail"))
//         {
//             int savedIndex = PlayerPrefs.GetInt("selectedTrail");
//             if (savedIndex < trails.Length)
//             {
//                 SelectTrail(trails[savedIndex]);
//             }
//         }
//     }

//     private int GetTrailCost(GameObject trailObj)
//     {
//         GameObject priceText = GetChildGameObject(trailObj, 0);
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
//             Debug.LogError($"Cannot get child {child} from trail object {(obj != null ? obj.name : "null")}");
//             return null;
//         }
//         return obj.transform.GetChild(child).gameObject;
//     }

//     private int GetTrailIndex(GameObject obj)
//     {
//         for (int i = 0; i < trails.Length; i++)
//         {
//             if (trails[i] == obj)
//                 return i;
//         }
//         return -1;
//     }

//     public void ClearSelection()
//     {
//         if (selectedTrail != null)
//         {
//             GetChildGameObject(selectedTrail, 1).SetActive(false);
//             selectedTrail = null;
//         }
//     }

//     public bool HasSelection()
//     {
//         return selectedTrail != null;
//     }

//     public GameObject GetSelectedTrail()
//     {
//         return selectedTrail;
//     }

//     private void OnScreenLeave()
//     {
//         if (selectedTrail != null)
//         {
//             int index = GetTrailIndex(selectedTrail);
//             PlayerPrefs.SetInt("trailStatus" + index, 1);
//         }
//     }
// }