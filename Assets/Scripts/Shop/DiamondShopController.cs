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
    [SerializeField] private Text diamondsText; 
    [SerializeField] private Button[] purchaseButtons; 

    [Header("Testing")]
    [SerializeField] private bool enableFakePurchases = true; 

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

    private void InitializeUI()
    {
        // Setup package UI elements
        for (int i = 0; i < diamondPackages.Length && i < purchaseButtons.Length; i++)
        {
            SetupPackageUI(i);
        }
    }

    private void SetupPackageUI(int index)
    {
        if (index < 0 || index >= diamondPackages.Length) return;

        DiamondPackage package = diamondPackages[index];
        
        if (package.packageUI != null)
        {
            Transform priceTextTransform = package.packageUI.transform.GetChild(0);
            if (priceTextTransform != null)
            {
                Text priceText = priceTextTransform.GetComponent<Text>();
                if (priceText != null)
                {
                    priceText.text = package.displayPrice;
                }
            }

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

    public void OnPackageClicked(int packageIndex)
    {
        if (packageIndex < 0 || packageIndex >= diamondPackages.Length)
        {
            return;
        }

        selectedPackage = diamondPackages[packageIndex];
        selectedPackageIndex = packageIndex;

        AttemptPurchase();
    }

    private void AttemptPurchase()
    {
        if (selectedPackage == null)
        {
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
            SimulatePurchaseSuccess();
        }
    }

    /// Simulate successful purchase (for testing)
    private void SimulatePurchaseSuccess()
    {
        if (selectedPackage == null) return;

        // Award diamonds
        if (DiamondManager.Instance != null)
        {
            DiamondManager.Instance.AddDiamonds(selectedPackage.diamondAmount);
        }

        // Clear selection
        selectedPackage = null;
        selectedPackageIndex = -1;
    }

    #endregion

    private void UpdateDiamondsDisplay()
    {
        if (diamondsText != null && DiamondManager.Instance != null)
        {
            diamondsText.text = DiamondManager.Instance.CurrentDiamonds.ToString();
        }
    }

    private void OnDiamondsChanged(int newDiamondCount)
    {
        UpdateDiamondsDisplay();
    }

    public void PurchasePackage(int packageIndex)
    {
        OnPackageClicked(packageIndex);
    }

    public string GetPackageInfo(int index)
    {
        if (index < 0 || index >= diamondPackages.Length) return "";
        
        DiamondPackage package = diamondPackages[index];
        return $"{package.packageName}\n{package.displayPrice}\n{package.diamondAmount} Diamonds";
    }
}