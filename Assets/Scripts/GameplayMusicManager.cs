using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameplayMusicManager : MonoBehaviour
{
    #region Singleton
    private static GameplayMusicManager _instance;
    public static GameplayMusicManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
            Destroy(this);
        else
            _instance = this;
    }
    #endregion

    [Header("Source")]
    [SerializeField] private AudioSource BGM;
    [SerializeField] private AudioSource zombiesSound;
    [SerializeField] private AudioSource soundEffect;
    [SerializeField] private AudioSource soundEffect2;
    [SerializeField] private AudioSource jumpSound;
    [SerializeField] private AudioSource rockCollide;
    [SerializeField] private AudioSource coin;
    [Header("Effect")]
    [SerializeField] private AudioClip boom;
    [SerializeField] private AudioClip eggToDragon;
    [SerializeField] private AudioClip henshin;
    [SerializeField] private AudioClip chestExplode;
    [SerializeField] private AudioClip goldenize;
    [Header("UI")]
    [SerializeField] private Slider musicUI;
    [SerializeField] public Slider SFXUI;


    // Start is called before the first frame update
    void Start()
    {
        if (!PlayerPrefs.HasKey("musicVolume"))
        {
            PlayerPrefs.SetFloat("musicVolume", 0.5f);
        }
        if (!PlayerPrefs.HasKey("SFXVolume"))
        {
            PlayerPrefs.SetFloat("SFXVolume", 0.5f);
        }
        
        LoadSound();
        
        ChangeMusicVolume();
        ChangeSFXVolume();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void StopBGMandZombie()
    {
        BGM.Stop();
        zombiesSound.Stop();
    }

    public void PlayBGMandZombie()
    {
        BGM.Play();
        zombiesSound.Play();
    }
    public void PauseBGMandZombie()
    {
        BGM.Pause();
        zombiesSound.Pause();
    }

    public void PlayBoomSound()
    {
        soundEffect.clip = boom;
        soundEffect.Play();
    }
    public void PlayEggToDragonSound()
    {
        soundEffect2.clip = eggToDragon;
        soundEffect2.Play();
    }
    public void PlayJumpSound()
    {
        jumpSound.Play();
    }
    public void PlayTransformSound()
    {
        rockCollide.Play();
        soundEffect.clip = henshin;
        soundEffect.PlayDelayed(1f);
    }
    public void PlayCarExplodeSound()
    {
        soundEffect.clip = chestExplode;
        soundEffect.Play();
    }
    public void PlayGoldenizeSound()
    {
        soundEffect.clip = goldenize;
        soundEffect.Play();
    }
    public void PlayDeTransformSound()
    {
        soundEffect.clip = henshin;
        soundEffect.Play();
    }
    public void ChangeMusicVolume()
    {
       BGM.volume = musicUI.value;
       zombiesSound.volume = musicUI.value;
       SaveSound();
    }

    public void ChangeSFXVolume()
    {
       soundEffect.volume = SFXUI.value;
       soundEffect2.volume = SFXUI.value;
       jumpSound.volume = SFXUI.value;
       coin.volume = SFXUI.value;
       rockCollide.volume = SFXUI.value;
       SaveSound();
    }

    private void LoadSound()
    {
        float musicVol = PlayerPrefs.GetFloat("musicVolume");
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume");
        
        musicUI.value = musicVol;
        SFXUI.value = sfxVol;
        BGM.volume = musicUI.value;
        zombiesSound.volume = musicUI.value;
        soundEffect.volume = SFXUI.value;
        soundEffect2.volume = SFXUI.value;
        jumpSound.volume = SFXUI.value;
        coin.volume = SFXUI.value;
        rockCollide.volume = SFXUI.value;
    }

    private void SaveSound()
    {
        PlayerPrefs.SetFloat("musicVolume", musicUI.value);
        PlayerPrefs.SetFloat("SFXVolume", SFXUI.value);
    }
}
