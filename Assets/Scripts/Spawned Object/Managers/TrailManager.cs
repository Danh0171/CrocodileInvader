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
        SpawnSelectedTrail();
    }

    void Update()
    {
        CheckTrailChange();
        
        ManageActiveTrail();
    }
    
    private void SpawnSelectedTrail()
    {
        // Check if player has selected a trail (if no key, no trail)
        if (!PlayerPrefs.HasKey("selectedTrail"))
        {
            return;
        }
        
        int selectedTrailID = PlayerPrefs.GetInt("selectedTrail", -1);
        
        // If selectedTrail is -1, means no trail wanted
        if (selectedTrailID < 0)
        {
            return;
        }
        
        // Check if selected trail is unlocked
        if (PlayerPrefs.GetInt("trailStatus" + selectedTrailID, 0) != 1)
        {
            return;
        }
        
        // Ensure we have trails to spawn
        if (PrefabsCount == 0) return;
        
        // Clamp ID to available prefabs
        selectedTrailID = Mathf.Clamp(selectedTrailID, 0, PrefabsCount - 1);
        
        // Spawn the selected trail
        currentActiveTrail = (Trail)GetItem(selectedTrailID);
    }
    
    private void CheckTrailChange()
    {
        // Check if player has selected a trail
        if (!PlayerPrefs.HasKey("selectedTrail"))
        {
            // No trail selected, remove current trail if any
            if (currentActiveTrail != null)
            {
                ReturnItem(currentActiveTrail);
                currentActiveTrail = null;
            }
            return;
        }
        
        int currentSelectedID = PlayerPrefs.GetInt("selectedTrail", -1);
        
        // If selectedTrail is -1, remove current trail
        if (currentSelectedID < 0)
        {
            if (currentActiveTrail != null)
            {
                ReturnItem(currentActiveTrail);
                currentActiveTrail = null;
            }
            return;
        }
        
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
    }
    
    private void ManageActiveTrail()
    {
        // If no dragons, remove trail
        if (GameManager.Instance.Zombies.FirstDragon == null && currentActiveTrail != null)
        {
            ReturnItem(currentActiveTrail);
            currentActiveTrail = null;
        }
        
    }
    
    public void ForceChangeTrail(int trailID)
    {
        PlayerPrefs.SetInt("selectedTrail", trailID);
        PlayerPrefs.Save();
        ChangeTrail(trailID);
    }
    
    public void DeselectTrail()
    {
        PlayerPrefs.DeleteKey("selectedTrail"); 
        PlayerPrefs.Save();
        
        if (currentActiveTrail != null)
        {
            ReturnItem(currentActiveTrail);
            currentActiveTrail = null;
        }
    }
}