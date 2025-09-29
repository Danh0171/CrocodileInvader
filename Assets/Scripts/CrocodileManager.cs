using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CrocodileManager : Manager
{
    public override PoolableObject GetItem(int id = 0)
    {
        Crocodile c = (Crocodile)base.GetItem(id);
        c.transform.position = transform.position;

        return c;
    }

    [SerializeField] private List<Crocodile> crocodileList = new List<Crocodile>();
    [SerializeField] private List<float> associatedX;
    [SerializeField] private int currentCrocodileID;
    private int layerCount = 1;
    private Crocodile firstCrocodile;
    [SerializeField] private float maxJumpDelay = 0.25f;
    private float bufferedJumpTimer = -1f;
    private const float JumpBufferDuration = 0.12f;
    private readonly Dictionary<Crocodile, bool> queuedJump = new Dictionary<Crocodile, bool>();

    #region Properties
    public Crocodile FirstCrocodile => firstCrocodile;

    public int Count => crocodileList.Count;
    public int CurrentFormID => currentCrocodileID;
    #endregion Properties

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        CollectingNulls();
        LoadCrocodileZombie();
        CrocodilesJumping();
        ProcessQueuedJumps();
        CalculatePosition();
        RearrangeCrocodiles();
    }

    private void LoadCrocodileZombie()
    {
        if (crocodileList.Count == 0) { firstCrocodile = null; return; }
        Crocodile groundedCandidate = null;
        float maxXGrounded = -GameManager.ScreenWidth;
        float maxXAny = -GameManager.ScreenWidth;
        Crocodile anyCandidate = null;

        foreach (Crocodile c in crocodileList)
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
        firstCrocodile = groundedCandidate != null ? groundedCandidate : anyCandidate;
    }

    private void CollectingNulls()
    {
        for (int i = 0; i < crocodileList.Count; i++)
            if (!crocodileList[i].isActiveAndEnabled) { crocodileList.RemoveAt(i); i--; }

    }

    public void AddZombie(bool isEating = false)
    {
        Crocodile c = (Crocodile)GetItem(currentCrocodileID);
        c.SetLayer(layerCount);

        if (isEating && GameManager.Instance.Zombies.FirstCrocodile)
            c.transform.position = GameManager.Instance.Zombies.FirstCrocodile.transform.position
                + new Vector3(-1f, 1f, 0f);

        if (layerCount == 1)
            layerCount = 3;
        else if (layerCount == 3)
            layerCount = 2;
        else
            layerCount = 1;

        crocodileList.Add(c);
        GameplayMusicManager.Instance.PlayChickenIntoCrocodileSound();
    }

    private bool AreAllOnGround()
    {
        foreach (Crocodile crocodile in crocodileList)
            if (crocodile.JumpStatus != 0) return false;
        return true;
    }

    private bool AreAllNotTouchingAnythingOtherThanGround()
    {
        foreach (Crocodile crocodile in crocodileList)
            if (crocodile.CollisionNumber > 0) return false;
        return true;
    }

    private float GetDelayedTime(Crocodile crocodile)
    {
        float delayModifier = (CurrentFormID == 1) ? 0.5f : 0.8f;
        float distance = Mathf.Max(FirstCrocodile.transform.position.x - crocodile.transform.position.x, 0f);
        float raw = distance / GameManager.Instance.ScrollBackSpeed * delayModifier + 0.001f;
        return Mathf.Min(maxJumpDelay, raw);
    }

    private void CrocodilesJumping()
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
        if (!FirstCrocodile) return;

        // Nếu đang buffer và đã có thể nhảy -> thực thi
        if (bufferedJumpTimer > 0f && FirstCrocodile.JumpStatus == 0)
        {
            TriggerGroupJump();
            bufferedJumpTimer = -1f;
        }

        // Giữ hành vi cũ nhưng không nuốt input: chỉ gọi nếu chưa dùng buffer
        if (pressBegan && bufferedJumpTimer < 0f && FirstCrocodile.JumpStatus == 0)
        {
            TriggerGroupJump();
        }

        // Kết thúc giữ -> rơi xuống
        if (pressEnded)
        {
            foreach (Crocodile crocodile in crocodileList)
                crocodile.CallTriggerFall(GetDelayedTime(crocodile));
        }

        // Trạng thái thả (không còn giữ) nhưng trước đó có cá sấu đang "IsTouchingScreen"
        if (!pressHeld)
        {
            foreach (Crocodile crocodile in crocodileList)
                if (crocodile.IsTouchingScreen)
                    crocodile.CallTriggerFall(GetDelayedTime(crocodile));
        }
    }

    private void TriggerGroupJump()
    {
        crocodileList.Sort((a, b) => b.transform.position.x.CompareTo(a.transform.position.x));

        foreach (Crocodile crocodile in crocodileList)
        {
            if (crocodile.IsOutGround) continue;

            if (crocodile.JumpStatus == 0)
            {
                crocodile.CallTriggerJump(GetDelayedTime(crocodile));
            }
            else
            {
                // Đang ở trên không: xếp hàng chờ đáp rồi nhảy lại
                if (!queuedJump.ContainsKey(crocodile))
                    queuedJump.Add(crocodile, true);
            }
        }
        GameplayMusicManager.Instance.PlayJumpSound();
    }

    private void CalculatePosition()
    {
        if (!FirstCrocodile || !AreAllOnGround()
            || !AreAllNotTouchingAnythingOtherThanGround()) return;
        associatedX = new List<float>();

        float availableWidth = 0.3f * GameManager.ScreenWidth;
        if (Count > 20)
            availableWidth = 0.4f * GameManager.ScreenWidth;
        float lastPossibleXPosition = -0.4f * GameManager.ScreenWidth;

        float d = availableWidth / crocodileList.Count;
        float maxD = 0.5f * FirstCrocodile.Width;
        if (maxD < d) d = maxD;

        for (int i = 0; i < crocodileList.Count; i++)
        {
            float distance = (crocodileList.Count - 1 - i) * d;
            associatedX.Add(lastPossibleXPosition + distance);
        }
    }

    private void RearrangeCrocodiles()
    {
        if (associatedX == null || associatedX.Count < crocodileList.Count) return;
        if (AreAllOnGround() && AreAllNotTouchingAnythingOtherThanGround())
        {
            for (int i = 0; i < crocodileList.Count; ++i)
            {
                if (crocodileList[i].transform.position.x == associatedX[i])
                    continue;
                float d = crocodileList[i].transform.position.x - associatedX[i];
                crocodileList[i].transform.position += 3 * d * Time.deltaTime * Vector3.left;
            }
        }
    }

    public void ChangeForm(int id)
    {
        if (id != 0)
            GameplayMusicManager.Instance.PlayGoldenizeSound();
        for (int i = 0; i < Count; ++i)
        {
            Crocodile temp = crocodileList[i];
            crocodileList[i] = (Crocodile)GetItem(id);
            crocodileList[i].transform.position = temp.transform.position + Vector3.up * 0.1f;
            crocodileList[i].SetLayer(temp.Layer);
            ReturnItem(temp);
        }
        currentCrocodileID = id;
    }

    private void ProcessQueuedJumps()
    {
        if (queuedJump.Count == 0) return;

        // Gom danh sách xóa để tránh sửa collection khi duyệt
        List<Crocodile> toRemove = null;

        foreach (var kv in queuedJump)
        {
            var c = kv.Key;
            if (!c || !c.isActiveAndEnabled)
            {
                (toRemove ??= new List<Crocodile>()).Add(c);
                continue;
            }

            // Khi đã chạm đất -> nhảy ngay (delay nhỏ để đảm bảo thứ tự gọi)
            if (c.JumpStatus == 0)
            {
                c.CallTriggerJump(0.001f);
                (toRemove ??= new List<Crocodile>()).Add(c);
            }
        }

        if (toRemove != null)
            foreach (var c in toRemove)
                queuedJump.Remove(c);
    }
}