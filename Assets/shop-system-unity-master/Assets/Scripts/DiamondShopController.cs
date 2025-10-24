using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiamondShopController : MonoBehaviour
{
    [System.Serializable]
    public class DiamondPackage
    {
        [Header("Package Info")]
        public string packageName = "Diamond Package";
        public string displayPrice = "$1.99";
        public int diamondAmount = 10;
        
        [Header("Package ID (for future IAP)")]
        public string packageID = "com.Redfox.10diamonds";
        
        [Header("UI References")]
        [Tooltip("GameObject chứa package UI - có cấu trúc giống Dragon/Trail items")]
        public GameObject packageUI;
    }

    [Header("Diamond Packages")]
    [SerializeField] private DiamondPackage[] diamondPackages = new DiamondPackage[]
    {
        new DiamondPackage { packageName = "Starter Pack", displayPrice = "$1.99", diamondAmount = 10 },
        new DiamondPackage { packageName = "Value Pack", displayPrice = "$4.99", diamondAmount = 30 },
        new DiamondPackage { packageName = "Popular Pack", displayPrice = "$9.99", diamondAmount = 55 },
        new DiamondPackage { packageName = "Great Deal", displayPrice = "$19.99", diamondAmount = 130 },
        new DiamondPackage { packageName = "Best Value", displayPrice = "$49.99", diamondAmount = 270 },
        new DiamondPackage { packageName = "Mega Pack", displayPrice = "$149.99", diamondAmount = 900 },
        new DiamondPackage { packageName = "Ultimate Pack", displayPrice = "$199.99", diamondAmount = 1500 }
    };

    [Header("UI References")]
    [SerializeField] private Text diamondsText; // Text hiển thị số diamonds hiện tại
    [SerializeField] private Button[] purchaseButtons; // Buttons để mua packages

    [Header("Testing")]
    [SerializeField] private bool enableFakePurchases = true; // Enable fake purchases for testing

    private DiamondPackage selectedPackage;
    private int selectedPackageIndex = -1;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeUI();
        SetupPackageButtons();
        UpdateDiamondsDisplay();
        
        // Subscribe to diamond changes
        if (DiamondManager.Instance != null)
        {
            DiamondManager.Instance.OnDiamondsChanged.AddListener(OnDiamondsChanged);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (DiamondManager.Instance != null)
        {
            DiamondManager.Instance.OnDiamondsChanged.RemoveListener(OnDiamondsChanged);
        }
    }

    #endregion

    #region UI Initialization

    /// <summary>
    /// Initialize UI elements
    /// </summary>
    private void InitializeUI()
    {
        // Setup package UI elements
        for (int i = 0; i < diamondPackages.Length && i < purchaseButtons.Length; i++)
        {
            SetupPackageUI(i);
        }
    }

    /// <summary>
    /// Setup individual package UI
    /// </summary>
    /// <param name="index">Package index</param>
    private void SetupPackageUI(int index)
    {
        if (index < 0 || index >= diamondPackages.Length) return;

        DiamondPackage package = diamondPackages[index];
        
        if (package.packageUI != null)
        {
            // Setup price text (child 0 - similar to Dragon/Trail structure)
            Transform priceTextTransform = package.packageUI.transform.GetChild(0);
            if (priceTextTransform != null)
            {
                Text priceText = priceTextTransform.GetComponent<Text>();
                if (priceText != null)
                {
                    priceText.text = package.displayPrice;
                }
            }

            // Setup diamond amount display (có thể là child khác)
            Text[] allTexts = package.packageUI.GetComponentsInChildren<Text>();
            foreach (Text text in allTexts)
            {
                if (text.name.ToLower().Contains("diamond") || text.name.ToLower().Contains("amount"))
                {
                    text.text = package.diamondAmount.ToString();
                    break;
                }
            }

            // Setup package name
            foreach (Text text in allTexts)
            {
                if (text.name.ToLower().Contains("name") || text.name.ToLower().Contains("title"))
                {
                    text.text = package.packageName;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Setup purchase buttons
    /// </summary>
    private void SetupPackageButtons()
    {
        for (int i = 0; i < purchaseButtons.Length && i < diamondPackages.Length; i++)
        {
            int packageIndex = i; // Capture for closure
            
            if (purchaseButtons[i] != null)
            {
                purchaseButtons[i].onClick.RemoveAllListeners();
                purchaseButtons[i].onClick.AddListener(() => OnPackageClicked(packageIndex));
            }
        }
    }

    #endregion

    #region Package Selection & Purchase

    /// <summary>
    /// Handle package click
    /// </summary>
    /// <param name="packageIndex">Index của package được click</param>
    public void OnPackageClicked(int packageIndex)
    {
        if (packageIndex < 0 || packageIndex >= diamondPackages.Length)
        {
            Debug.LogError($"DiamondShopController: Invalid package index {packageIndex}");
            return;
        }

        selectedPackage = diamondPackages[packageIndex];
        selectedPackageIndex = packageIndex;

        Debug.Log($"DiamondShopController: Selected package '{selectedPackage.packageName}' - {selectedPackage.displayPrice} for {selectedPackage.diamondAmount} diamonds");

        // Immediately attempt purchase (fake or real)
        AttemptPurchase();
    }

    /// <summary>
    /// Attempt to purchase selected package
    /// </summary>
    private void AttemptPurchase()
    {
        if (selectedPackage == null)
        {
            Debug.LogError("DiamondShopController: No package selected");
            return;
        }

        if (enableFakePurchases)
        {
            // Fake purchase for testing
            SimulatePurchaseSuccess();
        }
        else
        {
            // Real IAP purchase (sẽ implement sau)
            Debug.Log("DiamondShopController: Real IAP not implemented yet, using fake purchase");
            SimulatePurchaseSuccess();
        }
    }

    /// <summary>
    /// Simulate successful purchase (for testing)
    /// </summary>
    private void SimulatePurchaseSuccess()
    {
        if (selectedPackage == null) return;

        Debug.Log($"DiamondShopController: [FAKE PURCHASE] Successfully purchased {selectedPackage.packageName}");
        
        // Award diamonds
        if (DiamondManager.Instance != null)
        {
            DiamondManager.Instance.AddDiamonds(selectedPackage.diamondAmount);
        }
        else
        {
            Debug.LogError("DiamondShopController: DiamondManager instance not found!");
        }

        // Show success feedback (có thể add popup sau)
        ShowPurchaseSuccess();

        // Clear selection
        selectedPackage = null;
        selectedPackageIndex = -1;
    }

    /// <summary>
    /// Show purchase success feedback
    /// </summary>
    private void ShowPurchaseSuccess()
    {
        // TODO: Show success popup/animation
        Debug.Log("DiamondShopController: Purchase successful!");
        
        // For now, just log
        if (selectedPackage != null)
        {
            Debug.Log($"You received {selectedPackage.diamondAmount} diamonds!");
        }
    }

    #endregion

    #region UI Updates

    /// <summary>
    /// Update diamonds display
    /// </summary>
    private void UpdateDiamondsDisplay()
    {
        if (diamondsText != null && DiamondManager.Instance != null)
        {
            diamondsText.text = DiamondManager.Instance.CurrentDiamonds.ToString();
        }
    }

    /// <summary>
    /// Called when diamonds change
    /// </summary>
    /// <param name="newDiamondCount">New diamond count</param>
    private void OnDiamondsChanged(int newDiamondCount)
    {
        UpdateDiamondsDisplay();
    }

    #endregion

    #region Public Methods (for buttons/UI)

    /// <summary>
    /// Method để gọi từ button click events
    /// </summary>
    /// <param name="packageIndex">Package index</param>
    public void PurchasePackage(int packageIndex)
    {
        OnPackageClicked(packageIndex);
    }

    /// <summary>
    /// Get package info for UI
    /// </summary>
    /// <param name="index">Package index</param>
    /// <returns>Package info string</returns>
    public string GetPackageInfo(int index)
    {
        if (index < 0 || index >= diamondPackages.Length) return "";
        
        DiamondPackage package = diamondPackages[index];
        return $"{package.packageName}\n{package.displayPrice}\n{package.diamondAmount} Diamonds";
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// Debug method để test tất cả packages
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void Debug_TestAllPackages()
    {
        Debug.Log("DiamondShopController: Testing all packages...");
        
        for (int i = 0; i < diamondPackages.Length; i++)
        {
            Debug.Log($"Package {i}: {GetPackageInfo(i)}");
        }
    }

    /// <summary>
    /// Debug method để force purchase package
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void Debug_ForcePurchase(int packageIndex)
    {
        if (packageIndex >= 0 && packageIndex < diamondPackages.Length)
        {
            OnPackageClicked(packageIndex);
        }
    }

    #endregion
}