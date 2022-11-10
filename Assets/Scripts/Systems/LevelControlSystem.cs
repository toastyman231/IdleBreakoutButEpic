using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
    public int NumLevels;

    [BurstCompile]
    protected override void OnCreate()
    {
        base.OnCreate();
        EventQueue = new NativeQueue<int>(Allocator.Persistent);
        CheckLevelCompleteEvent += CheckLevelComplete;
        _levels = new NativeArray<FixedString32Bytes>(10, Allocator.Persistent);
        NumLevels = _levels.Length;
        for (int i = 0; i < 10; i++)
        {
            _levels[i] = "Level" + (i + 1);
        }
    }
    
    [BurstCompile]
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        // TODO: Make more levels
        //World.GetOrCreateSystem<GlobalDataUpdateSystem>().EventQueue.Enqueue(new GlobalDataEventArgs{ EventType = Field.LEVEL, NewData = 19 });
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
            //Debug.Log("1 Level: " + globalData.CurrentLevel);
        }
    }
    
    [BurstCompile]
    public void LoadLevel(float3[] brickPositions)
    {
        var globalData = GetSingleton<GlobalData>();
        //Debug.Log("2 Level: " + globalData.CurrentLevel);
        var prefab = ((globalData.CurrentLevel) % 20 == 0) ? GetSingleton<PrefabComponent>().GoldBrickPrefab : GetSingleton<PrefabComponent>().BrickPrefab;

        foreach (var position in brickPositions)
        {
            Entity entity = EntityManager.Instantiate(prefab);
            EntityManager.SetComponentData<Translation>(entity, new Translation{Value = position});
            int health = 0;
            if ((globalData.CurrentLevel) % 20 == 0) health = (globalData.CurrentLevel + 1) * 100;
            else if ((globalData.CurrentLevel) >= 1000) health = globalData.CurrentLevel * 2;
            else health = globalData.CurrentLevel;
            EntityManager.SetComponentData<BrickData>(entity, new BrickData
            {
                Health = health,
                Position = position
            });
            EntityManager.AddComponent<DoneSetupTag>(entity);
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
