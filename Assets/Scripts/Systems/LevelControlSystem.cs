using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

    private NativeArray<FixedString32Bytes> _levels;

    [BurstCompile]
    protected override void OnCreate()
    {
        base.OnCreate();
        EventQueue = new NativeQueue<int>(Allocator.Persistent);
        CheckLevelCompleteEvent += CheckLevelComplete;
        _levels = new NativeArray<FixedString32Bytes>(1, Allocator.Persistent);
        _levels[0] = "Level1";
    }
    
    [BurstCompile]
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        // TODO: Make more levels
        LoadLevel(Resources.Load<BrickPositionData>("Brick Layouts/Level1").positions);
    }

    [BurstCompile]
    protected override void OnDestroy()
    {
        EventQueue.Dispose();
        _levels.Dispose();
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
            BallShopControl.InvokeLevelLoadEvent();
            //Debug.Log("Level to load: " + (globalData.CurrentLevel + 1) % _levels.Length);
            LoadLevel(Resources.Load<BrickPositionData>("Brick Layouts/" + _levels[(globalData.CurrentLevel + 1) % _levels.Length]).positions);
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
    public void UnloadLevel()
    {
        EntityCommandBuffer.ParallelWriter commandBuffer =
            World.GetOrCreateSystem<EntityCommandBufferSystem>().CreateCommandBuffer().AsParallelWriter();
        Entities.ForEach((Entity entity, int nativeThreadIndex, ref BrickData brickData, in BrickTag tag) =>
        {
            commandBuffer.DestroyEntity(nativeThreadIndex, entity);
        }).ScheduleParallel();
        Dependency.Complete();
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
