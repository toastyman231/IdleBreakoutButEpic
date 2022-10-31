using System;
using System.ComponentModel;
using Systems;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

[BurstCompile]
public partial class BallSpawnSystem : SystemBase
{
    public event EventHandler<SpawnEventArgsClass> SpawnBallEvent;
    public NativeQueue<SpawnEventArgs> SpawnBallEventQueue;

    private Entity _basicBallPrefab;
    private Entity _plasmaBallPrefab;
    private Entity _sniperBallPrefab;
    private Entity _scatterBallPrefab;
    private Entity _scatterChildPrefab;

    private EntityManager _entityManager;
    //private readonly Random _rand = new Random();

    [BurstCompile]
    protected override void OnStartRunning()
    {
        PrefabComponent prefabComponent = GetSingleton<PrefabComponent>();
        SpawnBallEventQueue = new NativeQueue<SpawnEventArgs>(Allocator.Persistent);
        SpawnBallEvent += HandleBallSpawn;
        _entityManager = World.EntityManager;
        _basicBallPrefab = prefabComponent.BasicBallPrefab;
        _plasmaBallPrefab = prefabComponent.PlasmaBallPrefab;
        _sniperBallPrefab = prefabComponent.SniperBallPrefab;
        _scatterBallPrefab = prefabComponent.ScatterBallPrefab;
        _scatterChildPrefab = prefabComponent.ScatterChildPrefab;
    }
    
    [BurstCompile]
    protected override void OnUpdate()
    {
        while (SpawnBallEventQueue.TryDequeue(out var args))
        {
            SpawnBallEvent?.Invoke(this, new SpawnEventArgsClass{ Type = args.Type, Amount = args.Amount, Position = args.Position});
        }
    }

    [BurstCompile]
    protected override void OnDestroy()
    {
        base.OnDestroy();
        SpawnBallEventQueue.Dispose();
        SpawnBallEvent -= HandleBallSpawn;
    }

    [BurstCompile]
    private void HandleBallSpawn(object sender, SpawnEventArgsClass args)
    {
        NativeArray<BallType> types = new NativeArray<BallType>(1, Allocator.Temp);
        NativeArray<int> amounts = new NativeArray<int>(1, Allocator.Temp);
        types[0] = args.Type;
        amounts[0] = args.Amount;
        
        SpawnBalls(ref types, ref amounts, args.Position);
    }

    [BurstCompile]
    public void SpawnBalls(ref NativeArray<BallType> ballTypes, ref NativeArray<int> numToSpawn, float3 posToSpawn)
    {
        if (ballTypes.Length != numToSpawn.Length) return;

        for (int i = 0; i < ballTypes.Length; i++)
        {
            int currentCount = 0;
            switch (ballTypes[i])
            {
                case BallType.BasicBall:
                    currentCount = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<BallTag>())
                        .CalculateEntityCount();
                    if (currentCount > 0)
                    {
                        Entity basicEntity = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<BallTag>())
                            .ToEntityArray(Allocator.Temp)[0];
                        BasicBallSharedData curData = EntityManager.GetComponentData<BasicBallSharedData>(basicEntity);
                        
                        EntityManager.Instantiate(_basicBallPrefab, numToSpawn[i], Allocator.Temp);
                        int amount = numToSpawn[i];
                        Entities.ForEach((ref BasicBallSharedData sharedData, in BallTag tag) =>
                        {
                            sharedData.Speed = curData.Speed;
                            sharedData.Power = curData.Power;
                            sharedData.Cost = curData.Cost;
                            sharedData.Count = currentCount + amount;
                        }).ScheduleParallel();
                    }
                    else
                    {
                        EntityManager.Instantiate(_basicBallPrefab, numToSpawn[i], Allocator.Temp);
                        int amount = numToSpawn[i];
                        Entities.ForEach((ref BasicBallSharedData sharedData, in BallTag tag) =>
                        {
                            sharedData.Count += amount;
                        }).ScheduleParallel();
                    }
                    break;
                case BallType.PlasmaBall:
                    currentCount = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlasmaTag>())
                        .CalculateEntityCount();
                    if (currentCount > 0)
                    {
                        Entity plasmaEntity = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PlasmaTag>())
                            .ToEntityArray(Allocator.Temp)[0];
                        BasicBallSharedData curData = EntityManager.GetComponentData<BasicBallSharedData>(plasmaEntity);
                        PlasmaBallSharedData currentPlasma = EntityManager.GetComponentData<PlasmaBallSharedData>(plasmaEntity);

                        EntityManager.Instantiate(_plasmaBallPrefab, numToSpawn[i], Allocator.Temp);
                        int amount = numToSpawn[i];
                        Entities.ForEach((ref BasicBallSharedData sharedData, ref PlasmaBallSharedData plasmaData, in PlasmaTag tag) =>
                        {
                            sharedData.Speed = curData.Speed;
                            sharedData.Power = curData.Power;
                            sharedData.Cost = curData.Cost;
                            sharedData.Count = currentCount + amount;
                            plasmaData.Range = currentPlasma.Range;
                        }).ScheduleParallel();
                    }
                    else
                    {
                        EntityManager.Instantiate(_plasmaBallPrefab, numToSpawn[i], Allocator.Temp);
                        int amount = numToSpawn[i];
                        Entities.ForEach((ref BasicBallSharedData sharedData, in PlasmaTag tag) =>
                        {
                            sharedData.Count = currentCount + amount;
                        }).ScheduleParallel();
                    }
                    break;
                case BallType.SniperBall:
                    currentCount = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SniperTag>())
                        .CalculateEntityCount();
                    if (currentCount > 0)
                    {
                        Entity sniperEntity = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SniperTag>())
                            .ToEntityArray(Allocator.Temp)[0];
                        BasicBallSharedData curData = EntityManager.GetComponentData<BasicBallSharedData>(sniperEntity);
                        
                        EntityManager.Instantiate(_sniperBallPrefab, numToSpawn[i], Allocator.Temp);
                        int amount = numToSpawn[i];
                        Entities.ForEach((ref BasicBallSharedData sharedData, in SniperTag tag) =>
                        {
                            sharedData.Speed = curData.Speed;
                            sharedData.Power = curData.Power;
                            sharedData.Cost = curData.Cost;
                            sharedData.Count = currentCount + amount;
                        }).ScheduleParallel();
                    }
                    else
                    {
                        EntityManager.Instantiate(_sniperBallPrefab, numToSpawn[i], Allocator.Temp);
                        int amount = numToSpawn[i];
                        Entities.ForEach((ref BasicBallSharedData sharedData, in SniperTag tag) =>
                        {
                            sharedData.Count = currentCount + amount;
                        }).ScheduleParallel();
                    }
                    break;
                case BallType.ScatterBall:
                    currentCount = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ScatterTag>())
                        .CalculateEntityCount();
                    if (currentCount > 0)
                    {
                        Entity scatterEntity = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ScatterTag>())
                            .ToEntityArray(Allocator.Temp)[0];
                        BasicBallSharedData curData = EntityManager.GetComponentData<BasicBallSharedData>(scatterEntity);
                        ScatterBallSharedData currentScatter = EntityManager.GetComponentData<ScatterBallSharedData>(scatterEntity);

                        EntityManager.Instantiate(_scatterBallPrefab, numToSpawn[i], Allocator.Temp);
                        int amount = numToSpawn[i];
                        Entities.ForEach((ref BasicBallSharedData sharedData, ref ScatterBallSharedData scatterData, in ScatterTag tag) =>
                        {
                            sharedData.Speed = curData.Speed;
                            sharedData.Power = curData.Power;
                            sharedData.Cost = curData.Cost;
                            sharedData.Count = currentCount + amount;
                            scatterData.ExtraBalls = currentScatter.ExtraBalls;
                        }).ScheduleParallel();
                    }
                    else
                    {
                        EntityManager.Instantiate(_scatterBallPrefab, numToSpawn[i], Allocator.Temp);
                        int amount = numToSpawn[i];
                        Entities.ForEach((ref BasicBallSharedData sharedData, in ScatterTag tag) =>
                        {
                            sharedData.Count = currentCount + amount;
                        }).ScheduleParallel();
                    }
                    break;
                case BallType.ScatterChild:
                    Entity scatterBallEntity = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ScatterTag>())
                        .ToEntityArray(Allocator.Temp)[0];
                    BasicBallSharedData currentData = EntityManager.GetComponentData<BasicBallSharedData>(scatterBallEntity);

                    EntityManager.Instantiate(_scatterChildPrefab, numToSpawn[i], Allocator.Temp);
                    Entities.ForEach((ref BasicBallSharedData sharedData, in ScatterChildTag tag) =>
                    {
                        sharedData.Speed = Mathf.FloorToInt(currentData.Speed / 2f);
                        sharedData.Power = Mathf.FloorToInt(currentData.Power / 2f);
                    }).ScheduleParallel();
                    break;
                default:
                    Debug.Log("Unrecognized Ball Type!");
                    break;
            }
            
            new BallSetupJob
            {
                cmBuffer = World.GetOrCreateSystem<EntityCommandBufferSystem>().CreateCommandBuffer().AsParallelWriter(),
                SpawnTags = GetComponentDataFromEntity<RepositionOnSpawnTag>(),
                DeltaTime = Time.DeltaTime,
                GlobalData = GetSingleton<GlobalData>(),
                SpawnPos = posToSpawn,
                Rand = new Unity.Mathematics.Random((uint)DateTime.Now.Ticks),
                //RandomInCircle = Random.insideUnitCircle.normalized
            }.ScheduleParallel(Dependency).Complete();
            
            //World.GetOrCreateSystem<EntityCommandBufferSystem>().AddJobHandleForProducer(Dependency);
            //Dependency.Complete();
        }

        //.ScheduleParallel();
        ballTypes.Dispose();
        numToSpawn.Dispose();
    }
    
    [BurstCompile]
    private partial struct BallSetupJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter cmBuffer;
        [Unity.Collections.ReadOnlyAttribute] public ComponentDataFromEntity<RepositionOnSpawnTag> SpawnTags;
        public float DeltaTime;
        public GlobalData GlobalData;
        public float3 SpawnPos;
        public Unity.Mathematics.Random Rand;
        //public float2 RandomInCircle;
        
        [NativeSetThreadIndex]
        private int _threadIndex;
        
        [BurstCompile]
        void Execute(Entity entity, ref Translation translation, ref PhysicsVelocity pv, ref BasicBallSharedData ballData, in NewBallTag nbTag)
        {
            float randomAngle = math.radians(Rand.NextInt(1, 361));

            if (SpawnTags.HasComponent(entity))
            {
                translation.Value = SpawnPos;
            }

            //float3 direction = Random.insideUnitSphere.normalized;

            pv.Linear = new float3(math.cos(randomAngle) /*RandomInCircle.x*/ * ballData.Speed * GlobalData.GlobalSpeed * GlobalData.SpeedScale * DeltaTime, 
                math.sin(randomAngle) /*RandomInCircle.y*/ * ballData.Speed * GlobalData.GlobalSpeed * GlobalData.SpeedScale * DeltaTime, 0);
            //Debug.Log(string.Format("Set up: {0}", pv.Linear));
            
            cmBuffer.RemoveComponent<NewBallTag>(_threadIndex, entity);
            cmBuffer.AddComponent<DoneSetupTag>(_threadIndex, entity);
        }
    }
}

public enum BallType
{
    BasicBall = 0,
    PlasmaBall = 1,
    SniperBall = 2,
    ScatterBall = 3,
    ScatterChild = 6
}

public class SpawnEventArgsClass : EventArgs
{
    public BallType Type;
    public int Amount;
    public float3 Position;
}

public struct SpawnEventArgs
{
    public BallType Type;
    public int Amount;
    public float3 Position;
}
