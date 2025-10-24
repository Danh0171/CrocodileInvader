using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class DiamondManager : MonoBehaviour
{
    #region Singleton
    private static DiamondManager _instance;
    public static DiamondManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    [Header("Diamond Settings")]
    [SerializeField] private int diamondToScalesRate = 100; // 1 diamond = 100 scales

    [Header("Convert Toggle")]
    [SerializeField] private Button convertToggleIcon; // Icon/Button để toggle convert panel
    [SerializeField] private GameObject convertPanel; // Panel chứa tất cả convert UI

    [Header("Convert UI References")]
    [SerializeField] private TMP_InputField convertInputField; // TextMeshPro Input field cho số diamond
    [SerializeField] private Button convertButton; // Convert button  
    [SerializeField] private TextMeshProUGUI convertPreviewText; // Real-time preview "X Diamonds → Y Scales"
    [SerializeField] private TextMeshProUGUI convertFeedbackText; // TextMeshPro Text hiển thị feedback
    [SerializeField] private float feedbackDisplayTime = 3f; // Thời gian hiển thị feedback

    [Header("Events")]
    public UnityEvent<int> OnDiamondsChanged; // Event khi diamonds thay đổi
    public UnityEvent<int> OnScalesChanged;   // Event khi scales thay đổi từ conversion

    private int currentDiamonds;
    private bool isConvertPanelOpen = false; // Track convert panel state

    #region Properties
    public int CurrentDiamonds => currentDiamonds;
    public int DiamondToScalesRate => diamondToScalesRate;
    #endregion

    private void Start()
    {
        LoadDiamonds();
        SetupConvertUI();
    }

    #region Diamond Management
    
    /// <summary>
    /// Load diamond count từ PlayerPrefs
    /// </summary>
    private void LoadDiamonds()
    {
        currentDiamonds = PlayerPrefs.GetInt("totalDiamonds", 0);
        
        // Trigger event để update UI
        OnDiamondsChanged?.Invoke(currentDiamonds);
    }

    /// <summary>
    /// Save diamond count vào PlayerPrefs
    /// </summary>
    private void SaveDiamonds()
    {
        PlayerPrefs.SetInt("totalDiamonds", currentDiamonds);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Thêm diamonds (từ purchases)
    /// </summary>
    /// <param name="amount">Số diamonds cần thêm</param>
    public void AddDiamonds(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("DiamondManager: Cannot add negative or zero diamonds");
            return;
        }

        currentDiamonds += amount;
        SaveDiamonds();
        
        // Trigger event để update UI
        OnDiamondsChanged?.Invoke(currentDiamonds);
    }

    /// <summary>
    /// Spend diamonds (để mua trails)
    /// </summary>
    /// <param name="amount">Số diamonds cần spend</param>
    /// <returns>True nếu có đủ diamonds và spend thành công</returns>
    public bool SpendDiamonds(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("DiamondManager: Cannot spend negative or zero diamonds");
            return false;
        }

        if (currentDiamonds < amount)
        {
            Debug.LogWarning($"DiamondManager: Not enough diamonds. Need {amount}, have {currentDiamonds}");
            return false;
        }

        currentDiamonds -= amount;
        SaveDiamonds();
        
        // Trigger event để update UI
        OnDiamondsChanged?.Invoke(currentDiamonds);
        
        return true;
    }

    /// <summary>
    /// Check xem có đủ diamonds không
    /// </summary>
    /// <param name="amount">Số diamonds cần check</param>
    /// <returns>True nếu có đủ diamonds</returns>
    public bool HasEnoughDiamonds(int amount)
    {
        return currentDiamonds >= amount;
    }

    #endregion

    #region Diamond to Scales Conversion

    /// <summary>
    /// Convert diamonds sang scales
    /// </summary>
    /// <param name="diamondsToConvert">Số diamonds muốn convert</param>
    /// <returns>True nếu conversion thành công</returns>
    public bool ConvertDiamondsToScales(int diamondsToConvert)
    {
        if (diamondsToConvert <= 0)
        {
            Debug.LogWarning("DiamondManager: Cannot convert negative or zero diamonds");
            return false;
        }

        if (!HasEnoughDiamonds(diamondsToConvert))
        {
            Debug.LogWarning($"DiamondManager: Not enough diamonds to convert. Need {diamondsToConvert}, have {currentDiamonds}");
            return false;
        }

        // Spend diamonds
        if (!SpendDiamonds(diamondsToConvert))
        {
            return false;
        }

        // Add scales to game
        int scalesToAdd = diamondsToConvert * diamondToScalesRate;
        AddScalesToGame(scalesToAdd);

        return true;
    }

    /// <summary>
    /// Tính toán số scales sẽ nhận được từ diamond conversion
    /// </summary>
    /// <param name="diamonds">Số diamonds</param>
    /// <returns>Số scales sẽ nhận được</returns>
    public int CalculateScalesFromDiamonds(int diamonds)
    {
        return diamonds * diamondToScalesRate;
    }

    /// <summary>
    /// Add scales vào game system
    /// </summary>
    /// <param name="scalesToAdd">Số scales cần add</param>
    private void AddScalesToGame(int scalesToAdd)
    {
        // Get current scales (note: still using "totalCoins" key for compatibility)
        int currentScales = PlayerPrefs.GetInt("totalCoins", 0);
        
        // Add new scales
        int newTotal = currentScales + scalesToAdd;
        
        // Save back to PlayerPrefs
        PlayerPrefs.SetInt("totalCoins", newTotal);
        PlayerPrefs.Save();
        
        // Trigger event để update UI
        OnScalesChanged?.Invoke(newTotal);
    }

    #endregion

    #region Convert UI Methods

    /// <summary>
    /// Setup Convert UI elements
    /// </summary>
    private void SetupConvertUI()
    {
        // Setup convert toggle icon click event
        if (convertToggleIcon != null)
        {
            convertToggleIcon.onClick.RemoveAllListeners();
            convertToggleIcon.onClick.AddListener(OnConvertIconClicked);
        }

        // Setup convert button click event
        if (convertButton != null)
        {
            convertButton.onClick.RemoveAllListeners();
            convertButton.onClick.AddListener(OnConvertButtonClicked);
        }

        // Hide convert panel initially
        if (convertPanel != null)
        {
            convertPanel.SetActive(false);
            isConvertPanelOpen = false;
        }

        // Hide feedback text initially
        if (convertFeedbackText != null)
        {
            convertFeedbackText.text = "";
            convertFeedbackText.gameObject.SetActive(false);
        }

        // Setup input field placeholder (TextMeshPro)
        if (convertInputField != null && convertInputField.placeholder != null)
        {
            TextMeshProUGUI placeholder = convertInputField.placeholder.GetComponent<TextMeshProUGUI>();
            if (placeholder != null)
            {
                placeholder.text = "Enter diamonds to convert...";
            }

            // Setup input field change event for real-time preview
            convertInputField.onValueChanged.RemoveAllListeners();
            convertInputField.onValueChanged.AddListener(OnInputFieldChanged);
        }

        // Setup initial preview text
        if (convertPreviewText != null)
        {
            UpdatePreviewDisplay("");
        }
    }

    /// <summary>
    /// Public method để toggle convert panel khi click icon
    /// </summary>
    public void OnConvertIconClicked()
    {
        if (convertPanel == null)
        {
            Debug.LogWarning("DiamondManager: Convert panel not assigned!");
            return;
        }

        // Toggle panel state
        isConvertPanelOpen = !isConvertPanelOpen;
        convertPanel.SetActive(isConvertPanelOpen);

        // Clear input and hide feedback when opening
        if (isConvertPanelOpen)
        {
            if (convertInputField != null)
            {
                convertInputField.text = "";
            }
            
            if (convertFeedbackText != null)
            {
                convertFeedbackText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Called when input field value changes - for real-time preview
    /// </summary>
    /// <param name="value">Input field value</param>
    private void OnInputFieldChanged(string value)
    {
        UpdatePreviewDisplay(value);
    }

    /// <summary>
    /// Update preview display based on input
    /// </summary>
    /// <param name="input">Input string from field</param>
    private void UpdatePreviewDisplay(string input)
    {
        if (convertPreviewText == null) return;

        if (string.IsNullOrEmpty(input.Trim()))
        {
            // Show default conversion rate when empty
            convertPreviewText.text = $"1 Diamond = {diamondToScalesRate} Scales";
            convertPreviewText.color = Color.black;
            return;
        }

        if (int.TryParse(input, out int diamonds) && diamonds > 0)
        {
            int scales = diamonds * diamondToScalesRate;
            convertPreviewText.text = $"{diamonds} Diamonds → {scales} Scales";
            
            // Color coding: Green if affordable, Red if not enough diamonds
            if (diamonds <= currentDiamonds)
            {
                convertPreviewText.color = Color.green;
            }
            else
            {
                convertPreviewText.color = Color.red;
            }
        }
        else
        {
            // Invalid input
            convertPreviewText.text = "Please enter a valid number";
            convertPreviewText.color = Color.red;
        }
    }

    /// <summary>
    /// Public method để gọi từ Convert Button
    /// </summary>
    public void OnConvertButtonClicked()
    {
        if (convertInputField == null)
        {
            ShowFeedback("Convert input field not found!", Color.red);
            return;
        }

        // Parse input
        string inputText = convertInputField.text.Trim();
        
        if (string.IsNullOrEmpty(inputText))
        {
            ShowFeedback("Please enter number of diamonds!", Color.red);
            return;
        }

        // Try parse to int
        if (!int.TryParse(inputText, out int diamondsToConvert))
        {
            ShowFeedback("Please enter a valid number!", Color.red);
            return;
        }

        // Validate input
        if (diamondsToConvert <= 0)
        {
            ShowFeedback("Please enter a positive number!", Color.red);
            return;
        }

        if (diamondsToConvert > currentDiamonds)
        {
            ShowFeedback($"Not enough diamonds! You have {currentDiamonds}", Color.red);
            return;
        }

        // Perform conversion
        int scalesToReceive = CalculateScalesFromDiamonds(diamondsToConvert);
        
        if (ConvertDiamondsToScales(diamondsToConvert))
        {
            ShowFeedback($"Successfully converted {diamondsToConvert} Diamonds to {scalesToReceive} Scales!", Color.green);
            
            // Clear input field
            convertInputField.text = "";
        }
        else
        {
            ShowFeedback("Conversion failed! Please try again.", Color.red);
        }
    }

    /// <summary>
    /// Hiển thị feedback message với color
    /// </summary>
    /// <param name="message">Message để hiển thị</param>
    /// <param name="color">Màu của text</param>
    private void ShowFeedback(string message, Color color)
    {
        if (convertFeedbackText == null) return;

        convertFeedbackText.text = message;
        convertFeedbackText.color = color;
        convertFeedbackText.gameObject.SetActive(true);

        // Auto hide sau 3 giây
        StartCoroutine(HideFeedbackAfterDelay());
    }

    /// <summary>
    /// Coroutine để ẩn feedback sau delay
    /// </summary>
    private System.Collections.IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDisplayTime);
        
        if (convertFeedbackText != null)
        {
            convertFeedbackText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Public method để lấy conversion rate info cho UI
    /// </summary>
    public string GetConversionRateInfo()
    {
        return $"1 Diamond = {diamondToScalesRate} Scales";
    }

    /// <summary>
    /// Public method để close convert panel từ bên ngoài
    /// </summary>
    public void CloseConvertPanel()
    {
        if (convertPanel != null)
        {
            convertPanel.SetActive(false);
            isConvertPanelOpen = false;
        }
    }

    /// <summary>
    /// Public method để check trạng thái convert panel
    /// </summary>
    public bool IsConvertPanelOpen()
    {
        return isConvertPanelOpen;
    }

    #endregion

    #region Debug/Testing Methods

    /// <summary>
    /// Debug method để test - chỉ dùng trong development
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugAddDiamonds(int amount = 100)
    {
        AddDiamonds(amount);
    }

    /// <summary>
    /// Debug method để reset diamonds - chỉ dùng trong development
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugResetDiamonds()
    {
        currentDiamonds = 0;
        SaveDiamonds();
        OnDiamondsChanged?.Invoke(currentDiamonds);
    }

    /// <summary>
    /// Get diamond info for debugging
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Diamonds: {currentDiamonds}, Rate: 1 diamond = {diamondToScalesRate} scales";
    }

    #endregion
}