using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Box : PoolableObject
{
    [SerializeField] private BoxCollider2D boxCollider2D;
    [SerializeField] private int numberHumansContains;
    [SerializeField] private int numberDragonNeeded;
    private readonly HashSet<Dragon> dragonContacts = new HashSet<Dragon>();
    [SerializeField] private Slider slider;

    [SerializeField] private TextMeshProUGUI text;

    private bool turnedGold = false;

    public override void Init()
    {
        base.Init();
        dragonContacts.Clear();
        turnedGold = false;
    }

    public override float Width => boxCollider2D.size.x;
    public override float Height => boxCollider2D.size.y;
    public int CollisionCount => dragonContacts.Count;

    // Start is called before the first frame update
    protected override void Start()
    {

    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if (CollisionCount >= numberDragonNeeded)
        {
            GeneratePreys();
            GameplayMusicManager.Instance.PlayCarExplodeSound();
            GameManager.Instance.CallExplosion(false);
            RemoveSelf();
        }
        text.text = CollisionCount.ToString() + "/" + numberDragonNeeded.ToString();
        slider.value = (float)CollisionCount / numberDragonNeeded;

        if (!turnedGold && GameManager.Instance.Zombies.FirstDragon &&
            (transform.position - GameManager.Instance.Zombies.FirstDragon.transform.position).magnitude
            <= GameManager.ScreenWidth * 0.2f &&
            GameManager.Instance.Zombies.IsMagicForm)
            TurnIntoGold();
    }

    private void TurnIntoGold()
    {
        GameManager.Instance.Coins.TransformIntoCoin(this, false, ID);
        GameManager.Instance.Zombies.FirstDragon.PlayAttackAnimation();
        GameplayMusicManager.Instance.PlayGoldenizeSound();
        GeneratePreys();
        RemoveSelf();
        turnedGold = true;
    }

    private void GeneratePreys()
    {
        for (int i = 0; i < numberHumansContains; ++i)
        {
            Egg h = (Egg)GameManager.Instance.Humans.GetItem(Random.Range(0,
                GameManager.Instance.Humans.PrefabsCount));
            h.transform.position = gameObject.transform.position + Vector3.right * Width;
            h.SetLayer(Random.Range(1, 4));
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Zombie")) return;

        if (GameManager.Instance.Zombies.IsNormalForm)
        {
            var croc = collision.gameObject.GetComponentInParent<Dragon>();
            if (croc != null)
                dragonContacts.Add(croc);
        }
        else if (GameManager.Instance.Zombies.IsMagicForm)
        {
            GameManager.Instance.Coins.TransformIntoCoin(this, false, ID);
            RemoveSelf();
            GameManager.Instance.GenerateZombies(numberHumansContains);
            GameplayMusicManager.Instance.PlayGoldenizeSound();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        var croc = collision.gameObject.GetComponentInParent<Dragon>();
        if (croc != null)
            dragonContacts.Remove(croc);
    }

    private void OnDisable()
    {
        dragonContacts.Clear();
    }

    protected override void DestroyOnOutOfBounds()
    {
        if (transform.position.x + Width < GameManager.ScreenWidth * -0.55f)
            RemoveSelf();
        if (transform.position.y + Height < GameManager.ScreenHeight / -2)
            RemoveSelf();
    }
}