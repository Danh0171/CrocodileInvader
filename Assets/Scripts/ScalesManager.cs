using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScalesManager : Manager
{
    [Header("Scales Bonus System")]
    [SerializeField] private int countAward = 0;
    [SerializeField] private float gemTimeBonus = 0f;
    [SerializeField] private int BONUS_GEMS = 3;
    [SerializeField] private float TIME_BONUS_WINDOW = 0.2f;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Countdown gem time bonus
        if (gemTimeBonus > 0f)
        {
            gemTimeBonus -= Time.deltaTime;
            if (gemTimeBonus <= 0f)
            {
                // Reset khi hết thời gian
                ResetScalesBonus();
            }
        }
    }

    public override void CallSpawnItem()
    {
        GetItem(Random.Range(0, 4));
    }

    public void TransformIntoCoin(PoolableObject item, bool isBomb, int vehicleID)
    {
        int ID = 3;
        if (!isBomb)
            ID += vehicleID + 1;
        ScalesContainer cc = (ScalesContainer)GetItem(ID);
        cc.transform.position = item.transform.position;
    }
    
    // Called by Scales when collected
    public void ProcessScalesBonus()
    {
        // Reset time window to 2 seconds
        gemTimeBonus = TIME_BONUS_WINDOW;
        countAward++;
        
        // Check if completed 10 gems
        if (countAward >= 10)
        {
            // Award bonus gems
            for (int i = 0; i < BONUS_GEMS; i++)
            {
                GameManager.Instance.IncCoin();
            }
            
            GameplayMusicManager.Instance.PlayPerfectSound();
            
            // Reset for next bonus round
            ResetScalesBonus();
        }
    }
    
    private void ResetScalesBonus()
    {
        countAward = 0;
        gemTimeBonus = 0f;
    }
}
