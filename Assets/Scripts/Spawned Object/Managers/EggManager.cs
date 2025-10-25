using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EggManager : Manager
{
    public override void CallSpawnItem()
    {
        bool isSpawn4 = Random.Range(0, 10) == 0;
        if (isSpawn4)
        {
            for (int i = 0; i < 4; ++i)
            {
                Egg h = (Egg)GetItem(Random.Range(0, PrefabsCount));
                h.transform.position += Vector3.right * (i * h.Width);
                h.SetLayer(i == 3 ? 3 : 3 - i);
            }
        }
        else
        {
            Egg h = (Egg)GetItem(Random.Range(0, PrefabsCount));
            h.SetLayer(Random.Range(1, 4));
        }
    }
}
