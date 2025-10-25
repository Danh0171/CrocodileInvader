using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragonManager : Manager
{
    public override PoolableObject GetItem(int id = 0)
    {
        Dragon c = (Dragon)base.GetItem(id);
        c.transform.position = transform.position;

        return c;
    }

    [SerializeField] private List<Dragon> dragonList = new List<Dragon>();
    [SerializeField] private List<float> associatedX;
    [SerializeField] private bool isNormalForm = true; // true = normal dragons, false = magic dragon
    private int layerCount = 1;
    private Dragon firstDragon;
    [SerializeField] private float maxJumpDelay = 0.25f;
    private float bufferedJumpTimer = -1f;
    private const float JumpBufferDuration = 0.12f;
    private readonly Dictionary<Dragon, bool> queuedJump = new Dictionary<Dragon, bool>();

    #region Properties
    public Dragon FirstDragon => firstDragon;

    public int Count => dragonList.Count;
    public bool IsNormalForm => isNormalForm;
    public bool IsMagicForm => !isNormalForm;
    
    // Backward compatibility cho các scripts khác
    public int CurrentFormID => isNormalForm ? 0 : 1;
    #endregion Properties

    private int GetSelectedDragonID()
    {
        // Get all dragons in spawn pool and randomly select one
        List<int> poolDragons = new List<int>();
        
        // Check which dragons are in the spawn pool
        int normalDragonsCount = PrefabsCount - 1; // Exclude magic dragon at end
        for (int i = 0; i < normalDragonsCount; i++)
        {
            if (PlayerPrefs.GetInt("dragonInPool" + i, i == 0 ? 1 : 0) == 1)
            {
                poolDragons.Add(i);
            }
        }
        
        // If no dragons in pool, default to dragon 0
        if (poolDragons.Count == 0)
        {
            poolDragons.Add(0);
        }
        
        // Randomly select from pool
        int randomIndex = Random.Range(0, poolDragons.Count);
        return poolDragons[randomIndex];
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        CollectingNulls();
        LoadDragonZombie();
        DragonsJumping();
        ProcessQueuedJumps();
        CalculatePosition();
        RearrangeDragons();
    }

    private void LoadDragonZombie()
    {
        if (dragonList.Count == 0) { firstDragon = null; return; }
        Dragon groundedCandidate = null;
        float maxXGrounded = -GameManager.ScreenWidth;
        float maxXAny = -GameManager.ScreenWidth;
        Dragon anyCandidate = null;

        foreach (Dragon c in dragonList)
        {
            if (c.IsOutGround) continue;
            float x = c.transform.position.x;
            if (c.JumpStatus == 0 && x > maxXGrounded)
            {
                groundedCandidate = c;
                maxXGrounded = x;
            }
            if (x > maxXAny)
            {
                anyCandidate = c;
                maxXAny = x;
            }
        }
        firstDragon = groundedCandidate != null ? groundedCandidate : anyCandidate;
    }

    private void CollectingNulls()
    {
        for (int i = 0; i < dragonList.Count; i++)
            if (!dragonList[i].isActiveAndEnabled) { dragonList.RemoveAt(i); i--; }

    }

    public void AddZombie(bool isEating = false)
    {
        int prefabID; 
        
        if (isNormalForm)
        {
            prefabID = GetSelectedDragonID();
        }
        else 
        {
            prefabID = PrefabsCount - 1; 
        }
        
        Dragon c = (Dragon)GetItem(prefabID);
        c.SetLayer(layerCount);

        if (isEating && GameManager.Instance.Zombies.FirstDragon)
            c.transform.position = GameManager.Instance.Zombies.FirstDragon.transform.position
                + new Vector3(-1f, 1f, 0f);

        if (layerCount == 1)
            layerCount = 3;
        else if (layerCount == 3)
            layerCount = 2;
        else
            layerCount = 1;

        dragonList.Add(c);
        GameplayMusicManager.Instance.PlayEggToDragonSound();
    }

    private bool AreAllOnGround()
    {
        foreach (Dragon dragon in dragonList)
            if (dragon.JumpStatus != 0) return false;
        return true;
    }

    private bool AreAllNotTouchingAnythingOtherThanGround()
    {
        foreach (Dragon dragon in dragonList)
            if (dragon.CollisionNumber > 0) return false;
        return true;
    }

    private float GetDelayedTime(Dragon dragon)
    {
        float delayModifier = IsMagicForm ? 0.5f : 0.8f; 
        float distance = Mathf.Max(FirstDragon.transform.position.x - dragon.transform.position.x, 0f);
        float raw = distance / GameManager.Instance.ScrollBackSpeed * delayModifier + 0.001f;
        return Mathf.Min(maxJumpDelay, raw);
    }

    private void DragonsJumping()
    {
        bool pressBegan = false;
        bool pressEnded = false;
        bool pressHeld = false;
        bool pointerOverUI = false;

        // PC / Editor (mouse + phím)
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        if (EventSystem.current != null)
            pointerOverUI = EventSystem.current.IsPointerOverGameObject();

        pressBegan = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        pressEnded = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space);
        pressHeld = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
#endif

        // Mobile (touch)
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (EventSystem.current != null)
                pointerOverUI = EventSystem.current.IsPointerOverGameObject(t.fingerId);

            if (t.phase == TouchPhase.Began) pressBegan = true;
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) pressEnded = true;
            if (t.phase == TouchPhase.Stationary || t.phase == TouchPhase.Moved) pressHeld = true;
        }

        if (pressBegan)
            bufferedJumpTimer = JumpBufferDuration;

        if (bufferedJumpTimer > 0f)
            bufferedJumpTimer -= Time.deltaTime;

        if (pointerOverUI) return;
        if (!FirstDragon) return;

        // Nếu đang buffer và đã có thể nhảy -> thực thi
        if (bufferedJumpTimer > 0f && FirstDragon.JumpStatus == 0)
        {
            TriggerGroupJump();
            bufferedJumpTimer = -1f;
        }

        // Giữ hành vi cũ nhưng không nuốt input: chỉ gọi nếu chưa dùng buffer
        if (pressBegan && bufferedJumpTimer < 0f && FirstDragon.JumpStatus == 0)
        {
            TriggerGroupJump();
        }

        // Kết thúc giữ -> rơi xuống
        if (pressEnded)
        {
            foreach (Dragon dragon in dragonList)
                dragon.CallTriggerFall(GetDelayedTime(dragon));
        }

        // Trạng thái thả 
        if (!pressHeld)
        {
            foreach (Dragon dragon in dragonList)
                if (dragon.IsTouchingScreen)
                    dragon.CallTriggerFall(GetDelayedTime(dragon));
        }
    }

    private void TriggerGroupJump()
    {
        dragonList.Sort((a, b) => b.transform.position.x.CompareTo(a.transform.position.x));

        foreach (Dragon dragon in dragonList)
        {
            if (dragon.IsOutGround) continue;

            if (dragon.JumpStatus == 0)
            {
                dragon.CallTriggerJump(GetDelayedTime(dragon));
            }
            else
            {
                // Đang ở trên không: xếp hàng chờ đáp rồi nhảy lại
                if (!queuedJump.ContainsKey(dragon))
                    queuedJump.Add(dragon, true);
            }
        }
        GameplayMusicManager.Instance.PlayJumpSound();
    }

    private void CalculatePosition()
    {
        if (!FirstDragon || !AreAllOnGround()
            || !AreAllNotTouchingAnythingOtherThanGround()) return;
        associatedX = new List<float>();

        float availableWidth = 0.3f * GameManager.ScreenWidth;
        if (Count > 20)
            availableWidth = 0.4f * GameManager.ScreenWidth;
        float lastPossibleXPosition = -0.4f * GameManager.ScreenWidth;

        float d = availableWidth / dragonList.Count;
        float maxD = 0.5f * FirstDragon.Width;
        if (maxD < d) d = maxD;

        for (int i = 0; i < dragonList.Count; i++)
        {
            float distance = (dragonList.Count - 1 - i) * d;
            associatedX.Add(lastPossibleXPosition + distance);
        }
    }

    private void RearrangeDragons()
    {
        if (associatedX == null || associatedX.Count < dragonList.Count) return;
        if (AreAllOnGround() && AreAllNotTouchingAnythingOtherThanGround())
        {
            for (int i = 0; i < dragonList.Count; ++i)
            {
                if (dragonList[i].transform.position.x == associatedX[i])
                    continue;
                float d = dragonList[i].transform.position.x - associatedX[i];
                dragonList[i].transform.position += 3 * d * Time.deltaTime * Vector3.left;
            }
        }
    }

    public void ChangeForm(int id)
    {
        bool newIsNormalForm = (id == 0);
        ChangeForm(newIsNormalForm);
    }
    
    public void ChangeForm(bool toNormalForm)
    {
        if (!toNormalForm) // Chuyển sang magic form
            GameplayMusicManager.Instance.PlayGoldenizeSound();
            
        isNormalForm = toNormalForm;
        
        for (int i = 0; i < Count; ++i)
        {
            Dragon temp = dragonList[i];
            
            int prefabID;
            if (isNormalForm) 
            {
                prefabID = GetSelectedDragonID();
            }
            else 
            {
                prefabID = PrefabsCount - 1;
            }
            
            dragonList[i] = (Dragon)GetItem(prefabID);
            dragonList[i].transform.position = temp.transform.position + Vector3.up * 0.1f;
            dragonList[i].SetLayer(temp.Layer);
            ReturnItem(temp);
        }
    }

    private void ProcessQueuedJumps()
    {
        if (queuedJump.Count == 0) return;

        // Gom danh sách xóa để tránh sửa collection khi duyệt
        List<Dragon> toRemove = null;

        foreach (var kv in queuedJump)
        {
            var c = kv.Key;
            if (!c || !c.isActiveAndEnabled)
            {
                (toRemove ??= new List<Dragon>()).Add(c);
                continue;
            }

            // Khi đã chạm đất -> nhảy ngay (delay nhỏ để đảm bảo thứ tự gọi)
            if (c.JumpStatus == 0)
            {
                c.CallTriggerJump(0.001f);
                (toRemove ??= new List<Dragon>()).Add(c);
            }
        }

        if (toRemove != null)
            foreach (var c in toRemove)
                queuedJump.Remove(c);
    }
}