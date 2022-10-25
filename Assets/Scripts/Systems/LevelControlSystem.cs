using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile, UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class LevelControlSystem : SystemBase
{
    public event EventHandler CheckLevelCompleteEvent;
    public NativeQueue<int> EventQueue;
    
    [BurstCompile]
    protected override void OnCreate()
    {
        base.OnCreate();
        EventQueue = new NativeQueue<int>(Allocator.Persistent);
        CheckLevelCompleteEvent += CheckLevelComplete;
    }
    
    [BurstCompile]
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        // TODO: Replace with actual levels
        LoadLevel(Resources.Load<BrickPositionData>("Brick Layouts/Test").positions);
    }

    [BurstCompile]
    protected override void OnDestroy()
    {
        EventQueue.Dispose();
        CheckLevelCompleteEvent -= CheckLevelComplete;
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        while (EventQueue.TryDequeue(out int item))
        {
            CheckLevelCompleteEvent?.Invoke(this, EventArgs.Empty);
            EventQueue.Clear();
        }
    }

    [BurstCompile]
    private void CheckLevelComplete(object sender, EventArgs args)
    {
        //Debug.Log("Checking level complete");
        var globalData = GetSingleton<GlobalData>();
        if (globalData.Bricks <= 0)
        {
            //Debug.Log("No bricks");
            var globalDataUpdate = World.GetOrCreateSystem<GlobalDataUpdateSystem>();
            globalDataUpdate.EventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.MONEY, NewData = globalData.CurrentLevel * globalData.CashBonus});
            globalDataUpdate.EventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.LEVEL, NewData = globalData.CurrentLevel + 1});
            //TODO: Load next level
        }
    }
    
    [BurstCompile]
    public void LoadLevel(float3[] brickPositions)
    {
        var prefab = GetSingleton<PrefabComponent>().BrickPrefab;
        var globalData = GetSingleton<GlobalData>();

        foreach (var position in brickPositions)
        {
            Entity entity = EntityManager.Instantiate(prefab);
            EntityManager.SetComponentData<Translation>(entity, new Translation{Value = position});
            EntityManager.SetComponentData<BrickData>(entity, new BrickData
            {
                Health = globalData.CurrentLevel,
                Position = position
            });
        }
        
        World.GetOrCreateSystem<GlobalDataUpdateSystem>().EventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.BRICKS, NewData = brickPositions.Length});
    }
    
    [BurstCompile]
    public float3[] GetBrickPositions(List<float3> positionsList)
    {
        positionsList.Clear();

        Entities.ForEach((ref Translation translation, in BrickTag brickTag) =>
        {
            positionsList.Add(translation.Value);
        }).WithoutBurst().Run();

        return positionsList.ToArray();
    }
}
