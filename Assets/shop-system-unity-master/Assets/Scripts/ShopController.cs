using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopController : MonoBehaviour
{
    private ShopManager gameManager;

    public GameObject[] dragons; // Renamed from planets to dragons

    private GameObject selectedDragon; // Renamed from selectedPlanet

    [SerializeField]
    private Text value;

    [SerializeField]
    private Text coinsText;

    private int coins;

    private void Start()
    {
        LoadProgress(dragons);

        // load and select the saved dragon 
        // or select the first dragon if there is no saved value
        if (PlayerPrefs.HasKey("selectedDragon"))
        {
            SelectDragon(dragons[PlayerPrefs.GetInt("selectedDragon")]);
        }
        else
        {
            SelectDragon(dragons[0]);
        }

        // get the total coins from Dragon Age game
        coins = (PlayerPrefs.HasKey("totalCoins")) ? PlayerPrefs.GetInt("totalCoins") : 0;
        coinsText.text = coins.ToString();
    }

    public void OnClick(int index)
    {
        SelectDragon(dragons[index]);
    }

    public void Unlock()
    {
        int index = GetIndex(selectedDragon);
        int balance = coins - int.Parse(value.text);


        if (coins >= int.Parse(value.text) && (GetChildGameObject(selectedDragon, 3).activeSelf == true))
        {
            // withdraw the dragon cost from total coins and update text value
            PlayerPrefs.SetInt("totalCoins", balance);
            coinsText.text = balance.ToString();

            GetChildGameObject(selectedDragon, 3).SetActive(false);

            // Unlock dragon and automatically add to spawn pool
            PlayerPrefs.SetInt("dragonStatus" + index, 1);
            PlayerPrefs.SetInt("dragonInPool" + index, 1);
            
            // Show selection indicator
            GetChildGameObject(selectedDragon, 1).SetActive(true);
            
            PlayerPrefs.Save();
            
            Debug.Log($"Unlocked Dragon {index} and added to spawn pool!");
        }

    }

    private void SelectDragon(GameObject obj)
    {
        int index = GetIndex(obj);
        
        // Check if dragon is unlocked
        if (GetChildGameObject(obj, 3).activeSelf == true)
        {
            // Dragon is locked, show purchase UI
            if (selectedDragon)
                GetChildGameObject(selectedDragon, 1).SetActive(false);
            
            selectedDragon = obj;
            GetChildGameObject(obj, 1).SetActive(true);
            value.text = GetChildGameObject(obj, 0).GetComponent<Text>().text;
        }
        else
        {
            // Dragon is unlocked, toggle selection for pool
            bool currentlyEnabled = PlayerPrefs.GetInt("dragonInPool" + index, index == 0 ? 1 : 0) == 1;
            
            if (currentlyEnabled)
            {
                // Remove from pool
                PlayerPrefs.SetInt("dragonInPool" + index, 0);
                GetChildGameObject(obj, 1).SetActive(false); // Remove selection indicator
                Debug.Log($"Removed Dragon {index} from spawn pool");
            }
            else
            {
                // Add to pool
                PlayerPrefs.SetInt("dragonInPool" + index, 1);
                GetChildGameObject(obj, 1).SetActive(true); // Show selection indicator
                Debug.Log($"Added Dragon {index} to spawn pool");
            }
            
            PlayerPrefs.Save();
        }
    }

    // iterates through the list and unlocks dragons if the status equals 1
    private void LoadProgress(GameObject[] list)
    {
        for (int i = 0; i < list.Length; i++) // Fixed: should be < not <=
        {
            // Load unlock status
            if (PlayerPrefs.GetInt("dragonStatus" + i) == 1)
            {
                GetChildGameObject(list[i], 3).SetActive(false);
            }
            
            // Load selection status for spawn pool (only if unlocked)
            if (PlayerPrefs.GetInt("dragonStatus" + i) == 1)
            {
                bool inPool = PlayerPrefs.GetInt("dragonInPool" + i, i == 0 ? 1 : 0) == 1;
                GetChildGameObject(list[i], 1).SetActive(inPool);
            }
        }
        
        // Dragon 0 should always be unlocked and in pool (default)
        if (list.Length > 0)
        {
            GetChildGameObject(list[0], 3).SetActive(false);
            PlayerPrefs.SetInt("dragonStatus0", 1);
            PlayerPrefs.SetInt("dragonInPool0", 1);
            GetChildGameObject(list[0], 1).SetActive(true);
        }
    }

    // return a child game object from a game object
    private GameObject GetChildGameObject(GameObject obj, int child)
    {
        return obj.transform.GetChild(child).gameObject;
    }

    private void OnScreenLeave()
    {
        int index = GetIndex(selectedDragon);
        PlayerPrefs.SetInt("selectedDragon", index);
        PlayerPrefs.SetInt("dragonStatus" + index, 1);
    }

    // find the game object index and save
    private int GetIndex(GameObject obj)
    {
        int index = 0;

        while (obj != dragons[index])
        {
            index++;
        }

        return index;
    }
}