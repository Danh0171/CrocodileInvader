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
    [SerializeField] private Button convertToggleIcon; 
    [SerializeField] private GameObject convertPanel; 

    [Header("Convert UI References")]
    [SerializeField] private TMP_InputField convertInputField; 
    [SerializeField] private Button convertButton; 
    [SerializeField] private TextMeshProUGUI convertPreviewText; 
    [SerializeField] private TextMeshProUGUI convertFeedbackText; 
    [SerializeField] private float feedbackDisplayTime = 3f; 

    [Header("Events")]
    public UnityEvent<int> OnDiamondsChanged; 
    public UnityEvent<int> OnScalesChanged;   

    private int currentDiamonds;
    private bool isConvertPanelOpen = false; 

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
    
    private void LoadDiamonds()
    {
        currentDiamonds = PlayerPrefs.GetInt("totalDiamonds", 0);
        
        // Trigger event để update UI
        OnDiamondsChanged?.Invoke(currentDiamonds);
    }

    private void SaveDiamonds()
    {
        PlayerPrefs.SetInt("totalDiamonds", currentDiamonds);
        PlayerPrefs.Save();
    }

    /// <param name="amount">Số diamonds cần thêm</param>
    public void AddDiamonds(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentDiamonds += amount;
        SaveDiamonds();
        
        // Trigger event để update UI
        OnDiamondsChanged?.Invoke(currentDiamonds);
    }

    /// <param name="amount">Số diamonds cần spend</param>
    /// <returns>True nếu có đủ diamonds và spend thành công</returns>
    public bool SpendDiamonds(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (currentDiamonds < amount)
        {
            return false;
        }

        currentDiamonds -= amount;
        SaveDiamonds();
        
        // Trigger event để update UI
        OnDiamondsChanged?.Invoke(currentDiamonds);
        
        return true;
    }

    /// Check xem có đủ diamonds không
    /// <param name="amount">Số diamonds cần check</param>
    /// <returns>True nếu có đủ diamonds</returns>
    public bool HasEnoughDiamonds(int amount)
    {
        return currentDiamonds >= amount;
    }

    #endregion

    #region Diamond to Scales Conversion

    /// Convert diamonds sang scales
    /// <param name="diamondsToConvert">Số diamonds muốn convert</param>
    /// <returns>True nếu conversion thành công</returns>
    public bool ConvertDiamondsToScales(int diamondsToConvert)
    {
        if (diamondsToConvert <= 0)
        {
            return false;
        }

        if (!HasEnoughDiamonds(diamondsToConvert))
        {
            return false;
        }

        if (!SpendDiamonds(diamondsToConvert))
        {
            return false;
        }

        // Add scales to game
        int scalesToAdd = diamondsToConvert * diamondToScalesRate;
        AddScalesToGame(scalesToAdd);

        return true;
    }

    public int CalculateScalesFromDiamonds(int diamonds)
    {
        return diamonds * diamondToScalesRate;
    }

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

    private void SetupConvertUI()
    {
        if (convertToggleIcon != null)
        {
            convertToggleIcon.onClick.RemoveAllListeners();
            convertToggleIcon.onClick.AddListener(OnConvertIconClicked);
        }

        if (convertButton != null)
        {
            convertButton.onClick.RemoveAllListeners();
            convertButton.onClick.AddListener(OnConvertButtonClicked);
        }

        if (convertPanel != null)
        {
            convertPanel.SetActive(false);
            isConvertPanelOpen = false;
        }

        if (convertFeedbackText != null)
        {
            convertFeedbackText.text = "";
            convertFeedbackText.gameObject.SetActive(false);
        }

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

        if (convertPreviewText != null)
        {
            UpdatePreviewDisplay("");
        }
    }

    public void OnConvertIconClicked()
    {

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

    private void OnInputFieldChanged(string value)
    {
        UpdatePreviewDisplay(value);
    }

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

    private void ShowFeedback(string message, Color color)
    {
        if (convertFeedbackText == null) return;

        convertFeedbackText.text = message;
        convertFeedbackText.color = color;
        convertFeedbackText.gameObject.SetActive(true);

        // Auto hide sau 3 giây
        StartCoroutine(HideFeedbackAfterDelay());
    }

    private System.Collections.IEnumerator HideFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDisplayTime);
        
        if (convertFeedbackText != null)
        {
            convertFeedbackText.gameObject.SetActive(false);
        }
    }

    public string GetConversionRateInfo()
    {
        return $"1 Diamond = {diamondToScalesRate} Scales";
    }

    public void CloseConvertPanel()
    {
        if (convertPanel != null)
        {
            convertPanel.SetActive(false);
            isConvertPanelOpen = false;
        }
    }

    public bool IsConvertPanelOpen()
    {
        return isConvertPanelOpen;
    }

    #endregion

}