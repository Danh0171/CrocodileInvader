using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WitchManager : Manager
{
    public override void CallSpawnItem()
    {
        SpawnWitch();
    }

    private void SpawnWitch()
    {
        Witch witch = (Witch)GetItem();
        
        // Find road surface and spawn above it
        Vector3 spawnPos = GameManager.Instance.SpawnerPosition;
        LayerMask roadMask = LayerMask.GetMask("Road1");
        RaycastHit2D hit = Physics2D.Raycast(spawnPos, Vector2.down, 10f, roadMask);
        
        if (hit.collider != null)
        {
            spawnPos.x = GameManager.ScreenWidth*0.5f-2f;
            // Spawn above the road surface
            spawnPos.y = hit.collider.transform.position.y + 1.5f;
        }
        else
        {
            spawnPos.x = 18f - GameManager.ScreenWidth*0.5f-2f;
            // Fallback position if no road found
            spawnPos.y = Random.Range(-1f, 3f);
        }
        
        witch.transform.position = spawnPos;
    }
}
