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
    private Entity _basicBallPrefab;
    private Entity _plasmaBallPrefab;
    private Entity _sniperBallPrefab;
    //private readonly Random _rand = new Random();

    [BurstCompile]
    protected override void OnStartRunning()
    {
        PrefabComponent prefabComponent = GetSingleton<PrefabComponent>();
        _basicBallPrefab = prefabComponent.BasicBallPrefab;
        _plasmaBallPrefab = prefabComponent.PlasmaBallPrefab;
        _sniperBallPrefab = prefabComponent.SniperBallPrefab;
    }
    
    [BurstCompile]
    protected override void OnUpdate()
    {
        return;
    }
    
    [BurstCompile]
    public void SpawnBalls(ref NativeArray<BallType> ballTypes, ref NativeArray<int> numToSpawn)
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
                default:
                    Debug.Log("Unrecognized Ball Type!");
                    break;
            }
            
            new BallSetupJob
            {
                cmBuffer = World.GetOrCreateSystem<EntityCommandBufferSystem>().CreateCommandBuffer().AsParallelWriter(),
                DeltaTime = Time.DeltaTime,
                GlobalData = GetSingleton<GlobalData>(),
                //Rand = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks),
                RandomInCircle = Random.insideUnitCircle.normalized
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
        public float DeltaTime;
        public GlobalData GlobalData;
        //public Unity.Mathematics.Random Rand;
        public float2 RandomInCircle;
        
        [NativeSetThreadIndex]
        private int _threadIndex;
        
        [BurstCompile]
        void Execute(Entity entity, ref PhysicsVelocity pv, ref BasicBallSharedData ballData, in NewBallTag nbTag)
        {
            //float randomAngle = math.radians(Rand.NextInt(1, 361));

            //float3 direction = Random.insideUnitSphere.normalized;

            pv.Linear = new float3(/*math.cos(randomAngle)*/ RandomInCircle.x * ballData.Speed * GlobalData.GlobalSpeed * GlobalData.SpeedScale * DeltaTime, 
                /*math.sin(randomAngle)*/ RandomInCircle.y * ballData.Speed * GlobalData.GlobalSpeed * GlobalData.SpeedScale * DeltaTime, 0);
            //Debug.Log(string.Format("Set up: {0}", pv.Linear));
            
            cmBuffer.RemoveComponent<NewBallTag>(_threadIndex, entity);
        }
    }
}

public enum BallType
{
    BasicBall = 0,
    PlasmaBall = 1,
    SniperBall = 2
}
