using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadManager : Manager
{
    [SerializeField] private List<float> standardWidth = new List<float>();
    [SerializeField] private List<float> standardHeight = new List<float>();

    [Header("Watch only")]
    [SerializeField] private float countDistance;
    [SerializeField] private float standardDistance;
    [SerializeField] private float currentHeight;
    [SerializeField] private float nextHeight;
    [SerializeField] private int nextRoadID;
    [SerializeField] private bool lastSpawnWasCrackedRoad = false;

    public float MinHeight => standardHeight[0];

    public override PoolableObject GetItem(int id = 0)
    {
        return base.GetItem(id);
    }

    void Start()
    {
        countDistance = 0;
        currentHeight = standardHeight[0];
        nextHeight = currentHeight;
        nextRoadID = 2;
    }

    void Update()
    {
        standardDistance = GameManager.Instance.Zombies.FirstDragon ?
            GameManager.Instance.Zombies.FirstDragon.Width * 2f : 0;

        countDistance -= GameManager.Instance.ScrollBackSpeed * Time.deltaTime;
        if (countDistance <= 0)
            SpawnRoad();
    }

    private void SpawnRoad()
    {
        if (GameManager.Instance.SpawnBombAndWitchOnly)
            GameManager.Instance.SpawnBombAndWitchOnly = false;

        bool meetsBrainRequirement = GameManager.Instance.BrainNumber >= 30;
        bool isAllowedWidth = ((standardWidth[nextRoadID] == 4.5f || standardWidth[nextRoadID] == 35.16f) && nextHeight == standardHeight[1]);

        bool shouldSpawnCrackedRoad = meetsBrainRequirement &&
                                      isAllowedWidth;
        
        PoolableObject spawnedRoad;
        float width;
        int l = nextHeight == standardHeight[0] ? 0 : 1;
        
        if (shouldSpawnCrackedRoad && PrefabsCount > 1)
        {
            CrackedRoad cr = (CrackedRoad)GetItem(1);
            cr.Config(standardWidth[nextRoadID], l);
            
            cr.transform.position = GameManager.Instance.SpawnerPosition;
            cr.transform.position = new Vector3(cr.transform.position.x + cr.Width / 2,
                nextHeight, transform.position.z);
                
            spawnedRoad = cr;
            width = cr.Width;
            
            // Set flag để road kế tiếp sẽ có width lớn hơn
            lastSpawnWasCrackedRoad = true;
            
        }
        else
        {
            float roadWidth = standardWidth[nextRoadID];
            
            // Nếu road trước là CrackedRoad → tăng width để dễ landing
            if (lastSpawnWasCrackedRoad)
            {
                roadWidth = Mathf.Max(roadWidth * 1.5f, standardWidth[standardWidth.Count - 2]); 
                lastSpawnWasCrackedRoad = false; // Reset flag
            }
            
            Road r = (Road)GetItem(0);
            r.Config(roadWidth, l);

            r.transform.position = GameManager.Instance.SpawnerPosition;
            r.transform.position = new Vector3(r.transform.position.x + r.Width / 2,
                nextHeight, transform.position.z);
                
            spawnedRoad = r;
            width = r.Width;
        }

        if (width == standardWidth[^1])
            GameManager.Instance.SpawnBombAndWitchOnly = true;

        // Determine stat for next road
        float distance = standardDistance * GameManager.Instance.ScrollBackSpeed / 3f;
        nextHeight = standardHeight[Random.Range(0, standardHeight.Count)];

        int rd = Random.Range(0, 100);

        if (width == standardWidth[0])
            rd = 0;

        float m1 = 0f, m2 = 1f;
        if (rd < 50)
        {
            // Spawn normally
            nextRoadID = Random.Range(0, standardWidth.Count - 1);
            if (nextRoadID == 0 && width == standardWidth[0])
                nextHeight = currentHeight;
        }
        else if (rd < 50 + 35)
        {
            // Spawn overlap road
            m1 = -standardWidth[0] * 0.5f;
            m2 = 0f;
            if (currentHeight == standardHeight[0])
                nextHeight = standardHeight[1];
            else nextHeight = standardHeight[0];

            int rand = Random.Range(0, 100);
            if ((rand == 0 && GameManager.Instance.BrainNumber >= 15) || GameManager.Instance.Zombies.Count > 25)
                nextRoadID = standardWidth.Count - 1;
            else
                nextRoadID = Random.Range(1, standardWidth.Count - 1);
        }
        else
        {
            // Spawn right next road
            m2 = 0f;
        }

        if (nextHeight > currentHeight)
            m2 *= 0.5f;

        countDistance += (width + m1 + distance * m2);
        currentHeight = nextHeight;
    }
}
