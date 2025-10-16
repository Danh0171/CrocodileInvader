using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private GameObject Dragons;
    [SerializeField] private GameObject Trails;
    [SerializeField] private GameObject PowerUp;

    public Text coinsText;

    private int coins;

    private void Start()
    {
        // Sync với total coins từ gameplay
        this.coins = (PlayerPrefs.HasKey("totalCoins")) ? PlayerPrefs.GetInt("totalCoins") : 0;
        this.coinsText.text = coins.ToString();
    }

    public void ChangeCoins(bool isAdding)
    {
        if (isAdding)
        {
            this.coins += 50;
            SaveAndUpdate(this.coins);
        }
        else
        {
            if (this.coins > 0)
            {
                this.coins -= 50;
                SaveAndUpdate(this.coins);
            }
        }
    }

    private void SaveAndUpdate(int coins)
    {
        PlayerPrefs.SetInt("totalCoins", coins);
        this.coinsText.text = coins.ToString();
    }
    
    public bool TryPurchase(int cost)
    {
        if (this.coins >= cost)
        {
            this.coins -= cost;
            SaveAndUpdate(this.coins);
            return true;
        }
        return false;
    }

    public void NavigateTo(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void DragonPopUp()
    {
        Dragons.SetActive(true);
        Trails.SetActive(false);
        PowerUp.SetActive(false);
    }

    public void TrailsPopUp()
    {
        Trails.SetActive(true);
        Dragons.SetActive(false);
        PowerUp.SetActive(false);
    }

    public void PowerUpPopUp()
    {
        PowerUp.SetActive(true);
        Dragons.SetActive(false);
        Trails.SetActive(false);
    }
}
