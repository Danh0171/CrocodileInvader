using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScalesContainer : PoolableObject
{
    [SerializeField] private BoxCollider2D boxCollider;
    private List<Vector3> coinPosition;

    public override float Width => boxCollider.size.x;
    public override float Height => boxCollider.size.y;
    public override void Init()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(true);
            if (coinPosition == null)
            {
                coinPosition = new List<Vector3>();
                for (int j = 0; j < transform.childCount; ++j)
                    coinPosition.Add(transform.GetChild(j).position);
            }
            transform.GetChild(i).position = coinPosition[i];
        }
    }

    protected override void Update()
    {
        base.Update();
        if (ID == 4 && transform.position.x < -GameManager.ScreenWidth / 2f)
            GameManager.Instance.DeactivateTransform();
        if (ID == 4 && transform.position.x <= 0)
            GameplayMusicManager.Instance.PlayDeTransformSound();
    }
}
