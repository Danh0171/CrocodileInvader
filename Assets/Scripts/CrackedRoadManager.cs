using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrackedRoadManager : Manager
{
    [SerializeField] private int spawnRate = 15; // Tỷ lệ spawn (%)
    [SerializeField] private int minZombieCount = 8; // Số zombie tối thiểu để spawn

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public override void CallSpawnItem()
    {
        // Chỉ spawn khi có đủ zombie và theo tỷ lệ
        if (GameManager.Instance.Zombies.Count >= minZombieCount && 
            Random.Range(0, 100) < spawnRate)
        {
            GetItem();
        }
    }
}
