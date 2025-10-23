using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopController : MonoBehaviour
{
    private ShopManager gameManager;

    public GameObject[] dragons; // Renamed from planets to dragons
    public GameObject[] trails; // Trail items array

    private GameObject selectedDragon; // Renamed from selectedPlanet
    private GameObject selectedTrail; // For trail selection

    [SerializeField]
    private Text value;

    [SerializeField]
    private Text coinsText;

    private int coins;

    private void Start()
    {
        LoadProgress(dragons);
        LoadProgressTrails(trails);

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

        // load and select the saved trail
        // or select the first trail if there is no saved value
        if (PlayerPrefs.HasKey("selectedTrail"))
        {
            SelectTrail(trails[PlayerPrefs.GetInt("selectedTrail")]);
        }
        // else if (trails.Length > 0)
        // {
        //     SelectTrail(trails[0]);
        // }

        // get the total coins from Dragon Age game
        coins = (PlayerPrefs.HasKey("totalCoins")) ? PlayerPrefs.GetInt("totalCoins") : 0;
        coinsText.text = coins.ToString();
    }

    public void OnClick(int index)
    {
        SelectDragon(dragons[index]);
    }

    public void OnClickTrail(int index)
    {
        SelectTrail(trails[index]);
    }

    public void UnlockDragon()
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
            GameObject selectionIndicator = GetChildGameObject(selectedDragon, 1);
            selectionIndicator.SetActive(true);
            
            // Change image color to green to indicate selection
            Image indicatorImage = selectionIndicator.GetComponent<Image>();
            if (indicatorImage != null)
            {
                indicatorImage.color = Color.green;
            }
            
            PlayerPrefs.Save();
            
            Debug.Log($"Unlocked Dragon {index} and added to spawn pool!");
        }

    }

    public void UnlockTrail()
    {
        int index = GetTrailIndex(selectedTrail);
        int balance = coins - int.Parse(value.text);

        if (coins >= int.Parse(value.text) && (GetChildGameObject(selectedTrail, 3).activeSelf == true))
        {
            // withdraw the trail cost from total coins and update text value
            PlayerPrefs.SetInt("totalCoins", balance);
            coinsText.text = balance.ToString();

            GetChildGameObject(selectedTrail, 3).SetActive(false);

            // Unlock trail and set as selected
            PlayerPrefs.SetInt("trailStatus" + index, 1);
            PlayerPrefs.SetInt("selectedTrail", index);
            
            // Show selection indicator
            GameObject selectionIndicator = GetChildGameObject(selectedTrail, 1);
            selectionIndicator.SetActive(true);
            
            // Change image color to green to indicate selection
            Image indicatorImage = selectionIndicator.GetComponent<Image>();
            if (indicatorImage != null)
            {
                indicatorImage.color = Color.green;
            }
            
            PlayerPrefs.Save();
            
            Debug.Log($"Unlocked and selected Trail {index}!");
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
                GameObject selectionIndicator = GetChildGameObject(obj, 1);
                selectionIndicator.SetActive(true);
                
                // Change image color to green to indicate selection
                Image indicatorImage = selectionIndicator.GetComponent<Image>();
                if (indicatorImage != null)
                {
                    indicatorImage.color = Color.green;
                }
                Debug.Log($"Added Dragon {index} to spawn pool");
            }
            
            PlayerPrefs.Save();
        }
    }

    private void SelectTrail(GameObject obj)
    {
        int index = GetTrailIndex(obj);
        
        // Check if trail is unlocked
        if (GetChildGameObject(obj, 3).activeSelf == true)
        {
            // Trail is locked, show purchase UI
            if (selectedTrail)
                GetChildGameObject(selectedTrail, 1).SetActive(false);
            
            selectedTrail = obj;
            GetChildGameObject(obj, 1).SetActive(true);
            value.text = GetChildGameObject(obj, 0).GetComponent<Text>().text;
        }
        else
        {
            // Trail is unlocked, set as selected trail (only one trail can be active at a time)
            
            // Deselect all other trails first
            for (int i = 0; i < trails.Length; i++)
            {
                if (PlayerPrefs.GetInt("trailStatus" + i) == 1)
                {
                    GetChildGameObject(trails[i], 1).SetActive(false);
                }
            }
            
            // Select this trail
            PlayerPrefs.SetInt("selectedTrail", index);
            GameObject selectionIndicator = GetChildGameObject(obj, 1);
            selectionIndicator.SetActive(true);
            
            // Change image color to green to indicate selection
            Image indicatorImage = selectionIndicator.GetComponent<Image>();
            if (indicatorImage != null)
            {
                indicatorImage.color = Color.green;
            }
            
            PlayerPrefs.Save();
            Debug.Log($"Selected Trail {index}");
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
                GameObject selectionIndicator = GetChildGameObject(list[i], 1);
                selectionIndicator.SetActive(inPool);
                
                // Set green color for selected dragons
                if (inPool)
                {
                    Image indicatorImage = selectionIndicator.GetComponent<Image>();
                    if (indicatorImage != null)
                    {
                        indicatorImage.color = Color.green;
                    }
                }
            }
        }
        
        // Dragon 0 should always be unlocked and in pool (default)
        if (list.Length > 0)
        {
            GetChildGameObject(list[0], 3).SetActive(false);
            PlayerPrefs.SetInt("dragonStatus0", 1);
            PlayerPrefs.SetInt("dragonInPool0", 1);
            
            GameObject dragon0Indicator = GetChildGameObject(list[0], 1);
            dragon0Indicator.SetActive(true);
            
            // Set green color for dragon 0 (always selected)
            Image indicatorImage = dragon0Indicator.GetComponent<Image>();
            if (indicatorImage != null)
            {
                indicatorImage.color = Color.green;
            }
        }
    }

    // iterates through the trails and unlocks trails if the status equals 1
    private void LoadProgressTrails(GameObject[] list)
    {
        if (list == null || list.Length == 0) return;
        
        int selectedTrailIndex = PlayerPrefs.GetInt("selectedTrail", 0);
        
        for (int i = 0; i < list.Length; i++)
        {
            // Load unlock status
            if (PlayerPrefs.GetInt("trailStatus" + i) == 1)
            {
                GetChildGameObject(list[i], 3).SetActive(false);
            }
            
            // Load selection status (only one trail can be selected)
            if (PlayerPrefs.GetInt("trailStatus" + i) == 1)
            {
                bool isSelected = (selectedTrailIndex == i);
                GameObject selectionIndicator = GetChildGameObject(list[i], 1);
                selectionIndicator.SetActive(isSelected);
                
                // Set green color for selected trail
                if (isSelected)
                {
                    Image indicatorImage = selectionIndicator.GetComponent<Image>();
                    if (indicatorImage != null)
                    {
                        indicatorImage.color = Color.green;
                    }
                }
            }
        }
        
        // Trail 0 should always be unlocked and selected by default
        if (list.Length > 0)
        {
            GetChildGameObject(list[0], 3).SetActive(false);
            PlayerPrefs.SetInt("trailStatus0", 1);
            
            // If no trail is selected, select trail 0
            if (!PlayerPrefs.HasKey("selectedTrail"))
            {
                PlayerPrefs.SetInt("selectedTrail", 0);
            }
            
            bool isTrail0Selected = (selectedTrailIndex == 0);
            GameObject trail0Indicator = GetChildGameObject(list[0], 1);
            trail0Indicator.SetActive(isTrail0Selected);
            
            // Set green color for trail 0 if selected
            if (isTrail0Selected)
            {
                Image indicatorImage = trail0Indicator.GetComponent<Image>();
                if (indicatorImage != null)
                {
                    indicatorImage.color = Color.green;
                }
            }
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

    // find the trail index
    private int GetTrailIndex(GameObject obj)
    {
        int index = 0;

        while (index < trails.Length && obj != trails[index])
        {
            index++;
        }

        return index;
    }
}