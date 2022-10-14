using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerOld : MonoBehaviour
{
    public int NumToSpawn;

    public GameObject ObjectToSpawn;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < NumToSpawn; i++)
        {
            Instantiate(ObjectToSpawn, new Vector3(0, 0, 10), Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
