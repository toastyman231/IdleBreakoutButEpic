using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[BurstCompile]
public class ECSSpawn : MonoBehaviour
{
    [SerializeField] private GameObject _prefab;

    [SerializeField] private int numToSpawn;
    // Start is called before the first frame update
    [BurstCompile]
    void Start()
    {
        var prefab = Unity.Entities.GameObjectConversionUtility.ConvertGameObjectHierarchy(_prefab, 
            GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, new BlobAssetStore()));


        for (int i = 0; i < numToSpawn; i++)
        {
            World.DefaultGameObjectInjectionWorld.EntityManager.Instantiate(prefab);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
