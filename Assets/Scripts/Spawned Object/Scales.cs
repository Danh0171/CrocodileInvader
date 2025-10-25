using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scales : MonoBehaviour
{
    public AudioSource audioSource;
    [SerializeField] SpriteRenderer spriteRenderer;
    

    void Update()
    {
        if (GameManager.Instance.Zombies.IsMagicForm)
            MoveToZombie();
    }

    private void OnEnable()
    {
        spriteRenderer.enabled = true;
    }

    private void MoveToZombie()
    {
        Dragon target = GameManager.Instance.Zombies.FirstDragon;
        if (!target)
            return;
        Vector3 d = transform.position - target.transform.position;
        if (d.magnitude <= GameManager.ScreenWidth * 0.15f)
            transform.position -= 10f * Time.deltaTime * d;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (spriteRenderer.enabled && collision.gameObject.CompareTag("Zombie"))
        {
            if (audioSource)
            {
                audioSource.volume = GameplayMusicManager.Instance.SFXUI.value;
                audioSource.Play();
            }
            spriteRenderer.enabled = false;
            
            GameManager.Instance.IncCoin();
            
            GameManager.Instance.Coins.ProcessScalesBonus();
        }
    }
}
