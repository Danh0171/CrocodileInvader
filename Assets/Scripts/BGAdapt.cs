using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGAdapt : MonoBehaviour
{
    private float adapt = 20f;
    void Start()
    {
        adapt = GameManager.ScreenWidth;
        float length = this.GetComponent<SpriteRenderer>().bounds.size.x;
        length = adapt;
        Debug.Log("Length: " + length);
    }
}
