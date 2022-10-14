using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BallOld : MonoBehaviour
{
    public int Speed;

    private Rigidbody2D rb;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        float randomAngle = Mathf.Deg2Rad * Random.Range(1, 361);
        Vector3 dir = new Vector3(Mathf.Cos(randomAngle) * Speed, Mathf.Sin(randomAngle) * Speed, 0);
        rb.AddForce(dir, ForceMode2D.Impulse);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
