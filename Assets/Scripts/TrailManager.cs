using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class TrailManager : Manager
{
    public override PoolableObject GetItem(int id = 0)
    {
        Trail trail = (Trail)base.GetItem(id);
        trail.transform.position = GameManager.Instance.SpawnerPosition;
        return trail;
    }

    private Trail currentActiveTrail;
    
    #region Properties
    public Trail ActiveTrail => currentActiveTrail;
    public bool HasActiveTrail => currentActiveTrail != null && currentActiveTrail.gameObject.activeInHierarchy;
    #endregion Properties

    void Start()
    {
        // Spawn trail dựa trên selected trail from shop
        SpawnSelectedTrail();
    }

    void Update()
    {
        // Check if selected trail changed during gameplay
        CheckTrailChange();
        
        // Manage active trail
        ManageActiveTrail();
    }
    
    private void SpawnSelectedTrail()
    {
        int selectedTrailID = PlayerPrefs.GetInt("selectedTrail", 0);
        
        // Ensure we have trails to spawn
        if (PrefabsCount == 0) return;
        
        // Clamp ID to available prefabs
        selectedTrailID = Mathf.Clamp(selectedTrailID, 0, PrefabsCount - 1);
        
        // Spawn the selected trail
        currentActiveTrail = (Trail)GetItem(selectedTrailID);
        
        Debug.Log($"Spawned trail ID: {selectedTrailID}");
    }
    
    private void CheckTrailChange()
    {
        int currentSelectedID = PlayerPrefs.GetInt("selectedTrail", 0);
        
        if (currentActiveTrail != null)
        {
            // If trail changed, replace current trail
            if (currentActiveTrail.ID != currentSelectedID)
            {
                ChangeTrail(currentSelectedID);
            }
        }
        else if (GameManager.Instance.Zombies.FirstDragon != null)
        {
            // If no trail but have dragons, spawn trail
            SpawnSelectedTrail();
        }
    }
    
    private void ChangeTrail(int newTrailID)
    {
        // Remove current trail
        if (currentActiveTrail != null)
        {
            ReturnItem(currentActiveTrail);
            currentActiveTrail = null;
        }
        
        // Spawn new trail
        newTrailID = Mathf.Clamp(newTrailID, 0, PrefabsCount - 1);
        currentActiveTrail = (Trail)GetItem(newTrailID);
        
        Debug.Log($"Changed to trail ID: {newTrailID}");
    }
    
    private void ManageActiveTrail()
    {
        // If no dragons, remove trail
        if (GameManager.Instance.Zombies.FirstDragon == null && currentActiveTrail != null)
        {
            ReturnItem(currentActiveTrail);
            currentActiveTrail = null;
        }
        
        // If have dragons but no trail, spawn trail
        else if (GameManager.Instance.Zombies.FirstDragon != null && currentActiveTrail == null)
        {
            SpawnSelectedTrail();
        }
    }
    
    // Public method để force change trail (có thể gọi từ shop)
    public void ForceChangeTrail(int trailID)
    {
        PlayerPrefs.SetInt("selectedTrail", trailID);
        PlayerPrefs.Save();
        ChangeTrail(trailID);
    }
}