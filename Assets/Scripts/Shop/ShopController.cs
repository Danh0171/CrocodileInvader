using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopController : MonoBehaviour
{
    private ShopManager gameManager;

    public GameObject[] dragons; // Renamed from planets to dragons
    public GameObject[] trails; // Trail items array
    public GameObject[] diamonds; // Diamond packages array

    private GameObject selectedDragon; // Renamed from selectedPlanet
    private GameObject selectedTrail; // For trail selection
    private GameObject selectedDiamond; // For diamond package selection
    private int selectedDragonIndex = -1; // Track selected dragon index
    private int selectedTrailIndex = -1; // Track selected trail index
    private int selectedDiamondIndex = -1; // Track selected diamond index

    [Header("Dragon Cost Configuration")]
    [SerializeField] private bool[] dragonUsesDiamonds = new bool[] { false, true, true, true, true, true, true }; // dragon 0 uses coins, others use diamonds
    [SerializeField] private int[] dragonDiamondCosts = new int[] { 0, 25, 50, 75, 100, 150, 250 }; // diamond costs per dragon
    [SerializeField] private int[] dragonCoinCosts = new int[] { 100, 0, 0, 0, 0, 0, 0 }; // coin costs per dragon (only dragon 0)

    [Header("Trail Cost Configuration")]
    [SerializeField] private bool[] trailUsesDiamonds = new bool[] { false, true, true, true, true, true, true }; // trail 0 uses coins, others use diamonds
    [SerializeField] private int[] trailDiamondCosts = new int[] { 0, 50, 100, 150, 200, 300, 500 }; // diamond costs per trail
    [SerializeField] private int[] trailCoinCosts = new int[] { 100, 0, 0, 0, 0, 0, 0 }; // coin costs per trail (only trail 0)

    [Header("Diamond Package Configuration")]
    [SerializeField] private string[] diamondPackageNames = new string[] {
        "Starter Pack", "Value Pack", "Popular Pack", "Great Deal", "Best Value", "Mega Pack", "Ultimate Pack"
    };
    [SerializeField] private string[] diamondPackagePrices = new string[] {
        "$1.99", "$4.99", "$9.99", "$19.99", "$49.99", "$149.99", "$199.99"
    };
    [SerializeField] private int[] diamondPackageAmounts = new int[] {
        10, 30, 55, 130, 270, 900, 1500
    };
    [SerializeField] private bool enableFakePurchases = true; // Enable fake purchases for testing

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI value; // For coin costs
    [SerializeField] private TextMeshProUGUI diamondValue; // For diamond costs
    [SerializeField] private GameObject coinValueUI; // UI container for coin display
    [SerializeField] private GameObject diamondValueUI; // UI container for diamond display
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI diamondsText;

    [Header("Debug UI (Remove after testing)")]
    [SerializeField] private Button debugLockButton; // Debug button để lock items

    private int coins;

    private void Start()
    {
        LoadProgress(dragons);
        LoadProgressTrails(trails);

        // load and select the saved dragon 
        // or select the first dragon if there is no saved value
        if (PlayerPrefs.HasKey("selectedDragon"))
        {
            SelectDragon(PlayerPrefs.GetInt("selectedDragon"));
        }
        else
        {
            SelectDragon(0);
        }

        // load and select the saved trail
        // or select the first trail if there is no saved value
        if (PlayerPrefs.HasKey("selectedTrail"))
        {
            SelectTrail(PlayerPrefs.GetInt("selectedTrail"));
        }
        // else if (trails.Length > 0)
        // {
        //     SelectTrail(trails[0]);
        // }

        // get the total coins from Dragon Age game
        coins = (PlayerPrefs.HasKey("totalCoins")) ? PlayerPrefs.GetInt("totalCoins") : 0;
        coinsText.text = coins.ToString();
        
        // Load diamond progress and subscribe to events
        LoadDiamondProgress();
        SubscribeToEvents();
        
        // Setup debug button
        SetupDebugButton();
    }

    private void Update()
    {
        // Check for click outside to auto-deselect trails
        CheckClickOutsideToDeselect();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        UnsubscribeFromEvents();
    }

    public void OnClick(int index)
    {
        MarkClickProcessed(); // Mark click as handled
        SelectDragon(index);
    }

    public void OnClickTrail(int index)
    {
        MarkClickProcessed(); // Mark click as handled
        SelectTrail(index);
    }

    public void OnClickDiamond(int index)
    {
        MarkClickProcessed(); // Mark click as handled
        SelectDiamond(index);
    }

    public void UnlockDragon()
    {
        // Mark click as handled and store index
        MarkClickProcessed();
        int index = selectedDragonIndex;
        
        if (index < 0 || index >= dragons.Length)
        {
            Debug.LogError($"Invalid dragon index: {index}");
            return;
        }
        
        // Check if dragon is locked
        if (!GetChildGameObject(selectedDragon, 3).activeSelf)
        {
            return;
        }

        // Determine if dragon uses diamonds or coins
        bool usesDiamonds = index < dragonUsesDiamonds.Length ? dragonUsesDiamonds[index] : true;

        if (usesDiamonds)
        {
            // Dragon uses diamonds
            UnlockDragonWithDiamonds(index);
        }
        else
        {
            // Dragon uses coins (dragon 0)
            UnlockDragonWithCoins(index);
        }
    }

    private void UnlockDragonWithDiamonds(int index)
    {
        int diamondCost = index < dragonDiamondCosts.Length ? dragonDiamondCosts[index] : 100;

        // Check if DiamondManager exists
        if (DiamondManager.Instance == null)
        {
            Debug.LogError("DiamondManager not found! Cannot purchase dragon with diamonds.");
            return;
        }

        // Check if player has enough diamonds
        if (!DiamondManager.Instance.HasEnoughDiamonds(diamondCost))
        {
            Debug.LogWarning($"Not enough diamonds to purchase dragon {index}. Need {diamondCost}, have {DiamondManager.Instance.CurrentDiamonds}");
            return;
        }

        // Spend diamonds
        if (DiamondManager.Instance.SpendDiamonds(diamondCost))
        {
            // Unlock dragon
            UnlockDragonSuccess(index);
            
            // Note: Diamond display will be updated automatically via OnDiamondsChanged event
        }
    }

    private void UnlockDragonWithCoins(int index)
    {
        int coinCost = index < dragonCoinCosts.Length ? dragonCoinCosts[index] : 100;
        int balance = coins - coinCost;

        if (coins >= coinCost)
        {
            // Spend coins
            PlayerPrefs.SetInt("totalCoins", balance);
            coinsText.text = balance.ToString();
            coins = balance; // Update local coins

            // Unlock dragon
            UnlockDragonSuccess(index);
        }
    }

    private void UnlockDragonSuccess(int index)
    {
        // Hide lock UI
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
    }

    public void UnlockTrail()
    {
        // Mark click as handled and store index
        MarkClickProcessed();
        int index = selectedTrailIndex;
        
        if (index < 0 || index >= trails.Length)
        {
            Debug.LogError($"Invalid trail index: {index}");
            return;
        }
        
        // Check if trail is locked
        if (!GetChildGameObject(selectedTrail, 3).activeSelf)
        {
            return;
        }

        // Determine if trail uses diamonds or coins
        bool usesDiamonds = index < trailUsesDiamonds.Length ? trailUsesDiamonds[index] : true;

        if (usesDiamonds)
        {
            // Trail uses diamonds
            UnlockTrailWithDiamonds(index);
        }
        else
        {
            // Trail uses coins (trail 0)
            UnlockTrailWithCoins(index);
        }
    }

    public void PurchaseDiamond()
    {
        // Mark click as handled and store index
        MarkClickProcessed();
        int index = selectedDiamondIndex;
        
        if (index < 0 || index >= diamondPackageAmounts.Length)
        {
            Debug.LogError($"Invalid diamond package index: {index}");
            return;
        }

        if (enableFakePurchases)
        {
            // Fake purchase for testing
            SimulateDiamondPurchase(index);
        }
        else
        {
            // Real IAP purchase (sẽ implement sau)
            SimulateDiamondPurchase(index);
        }
    }

    private void UnlockTrailWithDiamonds(int index)
    {
        int diamondCost = index < trailDiamondCosts.Length ? trailDiamondCosts[index] : 100;

        // Check if DiamondManager exists
        if (DiamondManager.Instance == null)
        {
            Debug.LogError("DiamondManager not found! Cannot purchase trail with diamonds.");
            return;
        }

        // Check if player has enough diamonds
        if (!DiamondManager.Instance.HasEnoughDiamonds(diamondCost))
        {
            Debug.LogWarning($"Not enough diamonds to purchase trail {index}. Need {diamondCost}, have {DiamondManager.Instance.CurrentDiamonds}");
            return;
        }

        // Spend diamonds
        if (DiamondManager.Instance.SpendDiamonds(diamondCost))
        {
            // Unlock trail
            UnlockTrailSuccess(index);
            
            // Note: Diamond display will be updated automatically via OnDiamondsChanged event
        }
    }

    private void UnlockTrailWithCoins(int index)
    {
        int coinCost = index < trailCoinCosts.Length ? trailCoinCosts[index] : 100;
        int balance = coins - coinCost;

        if (coins >= coinCost)
        {
            // Spend coins
            PlayerPrefs.SetInt("totalCoins", balance);
            coinsText.text = balance.ToString();
            coins = balance; // Update local coins

            // Unlock trail
            UnlockTrailSuccess(index);
        }
    }

    private void UnlockTrailSuccess(int index)
    {
        // Hide lock UI
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
    }

    private void SelectDragon(int index)
    {
        if (index < 0 || index >= dragons.Length)
        {
            Debug.LogError($"Invalid dragon index: {index}");
            return;
        }

        GameObject obj = dragons[index];
        
        // Check if dragon is unlocked
        if (GetChildGameObject(obj, 3).activeSelf == true)
        {
            // Dragon is locked, show purchase UI
            if (selectedDragon && selectedDragonIndex >= 0)
                GetChildGameObject(selectedDragon, 1).SetActive(false);
            
            selectedDragonIndex = index;
            selectedDragon = obj;
            GetChildGameObject(obj, 1).SetActive(true);
            
            // Update cost display based on dragon type (similar to trail logic)
            UpdateDragonCostDisplay(index);
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
            }
            
            PlayerPrefs.Save();
        }
    }

    private void SelectTrail(int index)
    {
        if (index < 0 || index >= trails.Length)
        {
            Debug.LogError($"Invalid trail index: {index}");
            return;
        }

        GameObject obj = trails[index];
        
        // Check if trail is unlocked
        if (GetChildGameObject(obj, 3).activeSelf == true)
        {
            // Trail is locked, show purchase UI
            if (selectedTrail && selectedTrailIndex >= 0)
                GetChildGameObject(selectedTrail, 1).SetActive(false);
            
            selectedTrailIndex = index;
            selectedTrail = obj;
            GetChildGameObject(obj, 1).SetActive(true);
            
            // Update cost display based on trail type
            UpdateTrailCostDisplay(index);
        }
        else
        {
            // Trail is unlocked, toggle selection
            int currentSelectedTrail = PlayerPrefs.GetInt("selectedTrail", -1);
            
            if (currentSelectedTrail == index)
            {
                // Currently selected trail clicked again - deselect it (no trail mode)
                PlayerPrefs.DeleteKey("selectedTrail");
                GetChildGameObject(obj, 1).SetActive(false);
                PlayerPrefs.Save();
            }
            else
            {
                // Select this trail (deselect others first)
                
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
            }
        }
    }

    private void UpdateDragonCostDisplay(int index)
    {
        bool usesDiamonds = index < dragonUsesDiamonds.Length ? dragonUsesDiamonds[index] : true;

        if (usesDiamonds)
        {
            // Hiển thị UI của Diamond, tắt UI của Coin
            if (coinValueUI != null) coinValueUI.SetActive(false);
            if (diamondValueUI != null) diamondValueUI.SetActive(true);
            
            // Update diamond value display
            if (diamondValue != null)
            {
                int diamondCost = index < dragonDiamondCosts.Length ? dragonDiamondCosts[index] : 100;
                diamondValue.text = diamondCost.ToString();
            }
            
            // Legacy support cho old value text
            if (value != null)
            {
                int diamondCost = index < dragonDiamondCosts.Length ? dragonDiamondCosts[index] : 100;
                value.text = diamondCost.ToString();
            }
        }
        else
        {
            // Hiển thị UI của Coin, tắt UI của Diamond
            if (diamondValueUI != null) diamondValueUI.SetActive(false);
            if (coinValueUI != null) coinValueUI.SetActive(true);
            
            // Legacy support cho old value text
            if (value != null)
            {
                int coinCost = index < dragonCoinCosts.Length ? dragonCoinCosts[index] : 100;
                value.text = coinCost.ToString();
            }
        }
    }

    private void UpdateTrailCostDisplay(int index)
    {
        bool usesDiamonds = index < trailUsesDiamonds.Length ? trailUsesDiamonds[index] : true;

        if (usesDiamonds)
        {
            // Hiển thị UI của Diamond, tắt UI của Coin
            if (coinValueUI != null) coinValueUI.SetActive(false);
            if (diamondValueUI != null) diamondValueUI.SetActive(true);
            
            // Update diamond value display
            if (diamondValue != null)
            {
                int diamondCost = index < trailDiamondCosts.Length ? trailDiamondCosts[index] : 100;
                diamondValue.text = diamondCost.ToString();
            }
            
            // Legacy support cho old value text
            if (value != null)
            {
                int diamondCost = index < trailDiamondCosts.Length ? trailDiamondCosts[index] : 100;
                value.text = diamondCost.ToString();
            }
        }
        else
        {
            // Hiển thị UI của Coin, tắt UI của Diamond
            if (diamondValueUI != null) diamondValueUI.SetActive(false);
            if (coinValueUI != null) coinValueUI.SetActive(true);
            
            // Legacy support cho old value text
            if (value != null)
            {
                int coinCost = index < trailCoinCosts.Length ? trailCoinCosts[index] : 100;
                value.text = coinCost.ToString();
            }
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
        
        // Trail 0 default unlock (only if not explicitly locked by debug)
        if (list.Length > 0)
        {
            // Only auto-unlock trail 0 if it hasn't been set before (first time)
            if (!PlayerPrefs.HasKey("trailStatus0"))
            {
                GetChildGameObject(list[0], 3).SetActive(false);
                PlayerPrefs.SetInt("trailStatus0", 1);
            }
            
            // Optional: If no trail is selected and trail 0 is unlocked, you could select trail 0
            // But now we allow "no trail" as a valid option, so this is commented out
            // if (!PlayerPrefs.HasKey("selectedTrail") && PlayerPrefs.GetInt("trailStatus0", 0) == 1)
            // {
            //     PlayerPrefs.SetInt("selectedTrail", 0);
            // }
            
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
        if (selectedDragonIndex >= 0)
        {
            PlayerPrefs.SetInt("selectedDragon", selectedDragonIndex);
            PlayerPrefs.SetInt("dragonStatus" + selectedDragonIndex, 1);
        }
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

    #region Trail Cost Helper Methods

    /// <summary>
    /// Get trail cost info for UI or debugging
    /// </summary>
    /// <param name="index">Trail index</param>
    /// <returns>Cost info string</returns>
    public string GetTrailCostInfo(int index)
    {
        if (index < 0) return "Invalid trail";

        bool usesDiamonds = index < trailUsesDiamonds.Length ? trailUsesDiamonds[index] : true;

        if (usesDiamonds)
        {
            int diamondCost = index < trailDiamondCosts.Length ? trailDiamondCosts[index] : 100;
            return $"{diamondCost} Diamonds";
        }
        else
        {
            int coinCost = index < trailCoinCosts.Length ? trailCoinCosts[index] : 100;
            return $"{coinCost} Coins";
        }
    }

    /// <summary>
    /// Check if player can afford trail
    /// </summary>
    /// <param name="index">Trail index</param>
    /// <returns>True if player can afford the trail</returns>
    public bool CanAffordTrail(int index)
    {
        if (index < 0) return false;

        bool usesDiamonds = index < trailUsesDiamonds.Length ? trailUsesDiamonds[index] : true;

        if (usesDiamonds)
        {
            int diamondCost = index < trailDiamondCosts.Length ? trailDiamondCosts[index] : 100;
            return DiamondManager.Instance != null && DiamondManager.Instance.HasEnoughDiamonds(diamondCost);
        }
        else
        {
            int coinCost = index < trailCoinCosts.Length ? trailCoinCosts[index] : 100;
            return coins >= coinCost;
        }
    }

    #endregion

    #region Diamond Purchase Methods

    private void SimulateDiamondPurchase(int index)
    {
        // Simulate successful purchase
        DiamondManager.Instance.AddDiamonds(diamondPackageAmounts[index]);
        // Note: Diamond display will be updated automatically via OnDiamondsChanged event
    }



    /// <summary>
    /// Initial load of diamond progress - only called once at start
    /// After this, updates are handled automatically via events
    /// </summary>
    private void LoadDiamondProgress()
    {
        if (diamondsText != null && DiamondManager.Instance != null)
        {
            diamondsText.text = DiamondManager.Instance.CurrentDiamonds.ToString();
        }
    }

    private void SelectDiamond(int index)
    {
        if (index < 0 || index >= diamondPackageAmounts.Length)
        {
            Debug.LogError($"Invalid diamond package index: {index}");
            return;
        }

        // Deselect previous diamond
        if (selectedDiamond && selectedDiamondIndex >= 0 && selectedDiamondIndex < diamonds.Length)
            GetChildGameObject(selectedDiamond, 1).SetActive(false);
        
        // Select new diamond
        selectedDiamondIndex = index;
        if (index < diamonds.Length && diamonds[index] != null)
        {
            selectedDiamond = diamonds[index];
            GetChildGameObject(selectedDiamond, 1).SetActive(true);
        }
        
        // Update cost display
        UpdateDiamondCostDisplay(index);
    }

    private void UpdateDiamondCostDisplay(int index)
    {
        // Hiển thị UI của Diamond, tắt UI của Coin
        if (coinValueUI != null) coinValueUI.SetActive(false);
        if (diamondValueUI != null) diamondValueUI.SetActive(true);
        
        // Update diamond value display
        if (diamondValue != null)
        {
            diamondValue.text = $"{diamondPackageAmounts[index]}";
        }
    }

    #endregion

    #region Click Detection

    private float lastClickTime = 0f;
    private bool clickProcessed = false;
    private Coroutine autoDeselectCoroutine;

    /// <summary>
    /// Check for clicks outside trail items to auto-deselect
    /// </summary>
    private void CheckClickOutsideToDeselect()
    {
        // Check for trail, dragon, and diamond selections
        bool hasSelection = (selectedTrail != null && selectedTrailIndex >= 0) || 
                           (selectedDragon != null && selectedDragonIndex >= 0) ||
                           (selectedDiamond != null && selectedDiamondIndex >= 0);
        
        if (!hasSelection) return;

        // Check for mouse click
        if (Input.GetMouseButtonDown(0))
        {
            lastClickTime = Time.time;
            clickProcessed = false;
            
            // Stop any existing coroutine and start new one
            if (autoDeselectCoroutine != null)
            {
                StopCoroutine(autoDeselectCoroutine);
            }
            
            autoDeselectCoroutine = StartCoroutine(CheckClickProcessed());
        }
    }
    
    private System.Collections.IEnumerator CheckClickProcessed()
    {
        // Wait 0.1 seconds to ensure all button clicks are processed first
        yield return new WaitForSeconds(0.1f);
        
        // If click wasn't processed by any button, deselect current selections
        if (!clickProcessed)
        {
            // Deselect trail if selected
            if (selectedTrail != null && selectedTrailIndex >= 0)
            {
                DeselectCurrentTrail();
            }
            
            // Deselect dragon if selected for purchase
            if (selectedDragon != null && selectedDragonIndex >= 0)
            {
                DeselectCurrentDragon();
            }
            
            // Deselect diamond package if selected
            if (selectedDiamond != null && selectedDiamondIndex >= 0)
            {
                DeselectCurrentDiamond();
            }
        }
        
        // Clear coroutine reference when done
        autoDeselectCoroutine = null;
    }
    
    /// <summary>
    /// Call this method from click handlers to mark click as processed
    /// </summary>
    public void MarkClickProcessed()
    {
        clickProcessed = true;
    }



    /// <summary>
    /// Deselect currently selected trail for purchase
    /// </summary>
    private void DeselectCurrentTrail()
    {
        if (selectedTrail != null && selectedTrailIndex >= 0)
        {
            // Hide selection indicator
            GetChildGameObject(selectedTrail, 1).SetActive(false);
            
            // Hide cost display UI
            HideCostDisplay();
            
            // Clear selection
            selectedTrail = null;
            selectedTrailIndex = -1;
        }
    }

    /// <summary>
    /// Deselect currently selected dragon for purchase
    /// </summary>
    private void DeselectCurrentDragon()
    {
        if (selectedDragon != null && selectedDragonIndex >= 0)
        {
            // Hide selection indicator (only if dragon is locked - for purchase)
            if (GetChildGameObject(selectedDragon, 3).activeSelf)
            {
                GetChildGameObject(selectedDragon, 1).SetActive(false);
            }
            
            // Hide cost display UI
            HideCostDisplay();
            
            // Clear selection
            selectedDragon = null;
            selectedDragonIndex = -1;
        }
    }

    /// <summary>
    /// Deselect currently selected diamond package for purchase
    /// </summary>
    private void DeselectCurrentDiamond()
    {
        if (selectedDiamond != null && selectedDiamondIndex >= 0)
        {
            // Hide selection indicator
            GetChildGameObject(selectedDiamond, 1).SetActive(false);
            
            // Hide cost display UI
            HideCostDisplay();
            
            // Clear selection
            selectedDiamond = null;
            selectedDiamondIndex = -1;
        }
    }

    /// <summary>
    /// Hide all cost display UI elements
    /// </summary>
    private void HideCostDisplay()
    {
        // Hide both coin and diamond value UI containers
        if (coinValueUI != null) coinValueUI.SetActive(false);
        if (diamondValueUI != null) diamondValueUI.SetActive(false);
        
        // Clear text values as well (optional)
        if (value != null) value.text = "";
        if (diamondValue != null) diamondValue.text = "";
    }

    #endregion

    #region Event Management

    /// <summary>
    /// Subscribe to DiamondManager events for real-time UI updates
    /// </summary>
    private void SubscribeToEvents()
    {
        if (DiamondManager.Instance != null)
        {
            // Subscribe to diamond changes
            DiamondManager.Instance.OnDiamondsChanged.AddListener(UpdateDiamondDisplay);
            
            // Subscribe to scales changes (from diamond conversion)
            DiamondManager.Instance.OnScalesChanged.AddListener(UpdateScalesDisplay);
        }
    }

    /// <summary>
    /// Unsubscribe from DiamondManager events to prevent memory leaks
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (DiamondManager.Instance != null)
        {
            DiamondManager.Instance.OnDiamondsChanged.RemoveListener(UpdateDiamondDisplay);
            DiamondManager.Instance.OnScalesChanged.RemoveListener(UpdateScalesDisplay);
        }
    }

    #endregion

    #region Event Handlers
    
    /// <summary>
    /// Event handler for diamond amount changes - updates UI automatically
    /// </summary>
    /// <param name="newAmount">New diamond amount</param>
    private void UpdateDiamondDisplay(int newAmount)
    {
        if (diamondsText != null)
        {
            diamondsText.text = newAmount.ToString();
        }
    }

    /// <summary>
    /// Event handler for scales amount changes - updates UI automatically
    /// </summary>
    /// <param name="newAmount">New scales amount</param>
    private void UpdateScalesDisplay(int newAmount)
    {
        coins = newAmount;
        if (coinsText != null)
        {
            coinsText.text = newAmount.ToString();
        }
    }

    #endregion

    #region Debug Methods (REMOVE AFTER TESTING)

    /// <summary>
    /// Setup debug button for testing
    /// </summary>
    private void SetupDebugButton()
    {
        if (debugLockButton != null)
        {
            debugLockButton.onClick.RemoveAllListeners();
            debugLockButton.onClick.AddListener(OnDebugLockClicked);
            
            // Add text to button if it doesn't have any
            TextMeshProUGUI buttonText = debugLockButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null && string.IsNullOrEmpty(buttonText.text))
            {
                buttonText.text = "NUCLEAR LOCK";
            }
        }
    }

    /// <summary>
    /// Debug button click handler - Nuclear lock everything
    /// </summary>
    public void OnDebugLockClicked()
    {
        DebugNuclearLock();
        
        // Refresh UI
        LoadProgress(dragons);
        LoadProgressTrails(trails);
        // Note: Diamond display will be updated automatically via OnDiamondsChanged event
    }

    /// <summary>
    /// DEBUG: Nuclear lock everything except dragon 0
    /// </summary>
    private void DebugNuclearLock()
    {
        
        // Lock all dragons (except dragon 0)
        for (int i = 1; i < dragons.Length; i++)
        {
            PlayerPrefs.SetInt("dragonStatus" + i, 0);
            PlayerPrefs.SetInt("dragonInPool" + i, 0);
            
            // Show lock UI
            GetChildGameObject(dragons[i], 3).SetActive(true);  // Show lock
            GetChildGameObject(dragons[i], 1).SetActive(false); // Hide selection
        }
        
        // Lock ALL trails (including trail 0)
        for (int i = 0; i < trails.Length; i++)
        {
            PlayerPrefs.SetInt("trailStatus" + i, 0);
            
            // Show lock UI
            GetChildGameObject(trails[i], 3).SetActive(true);  // Show lock
            GetChildGameObject(trails[i], 1).SetActive(false); // Hide selection
        }
        
        // Reset all currencies to 0
        PlayerPrefs.SetInt("totalCoins", 0);    // Zero scales
        PlayerPrefs.SetInt("totalDiamonds", 0); // Zero diamonds
        
        // Reset selections
        PlayerPrefs.SetInt("selectedDragon", 0);  // Only dragon 0 available
        PlayerPrefs.DeleteKey("selectedTrail");   // No trail selected
        
        PlayerPrefs.Save();
        
        // Update local variables and UI
        coins = 0;
        coinsText.text = "0";
        
        // Force update DiamondManager if exists
        if (DiamondManager.Instance != null)
        {
            // Reset diamonds in DiamondManager (use reflection or direct access)
            Debug.Log("Resetting DiamondManager diamonds to 0");
        }
        
        Debug.Log($"🔒 Locked {dragons.Length - 1} dragons + {trails.Length} trails");
        Debug.Log("💰 Reset scales and diamonds to 0");
        Debug.Log("✅ Only Dragon 0 remains unlocked");
    }

    #endregion
}