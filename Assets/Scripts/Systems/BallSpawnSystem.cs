using Systems;
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
    //private readonly Random _rand = new Random();

    [BurstCompile]
    protected override void OnStartRunning()
    {
        _basicBallPrefab = GetSingleton<PrefabComponent>().BasicBallPrefab;
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
            switch (ballTypes[i])
            {
                case BallType.BasicBall:
                    EntityManager.Instantiate(_basicBallPrefab, numToSpawn[i], Allocator.Temp);
                    BasicBallSharedData currentData = GetSingleton<BasicBallSharedData>();
                    World.GetOrCreateSystem<BallSharedDataUpdateSystem>().InvokeUpdateSharedDataEvent(currentData.Power,
                        currentData.Speed, currentData.Cost, currentData.Count + numToSpawn[i]);
                    break;
                default:
                    Debug.Log("Unrecognized Ball Type!");
                    break;
            }
        }

        var ballSetupJob = new BallSetupJob
        {
            cmBuffer = World.GetOrCreateSystem<EntityCommandBufferSystem>().CreateCommandBuffer().AsParallelWriter(),
            DeltaTime = Time.DeltaTime,
            GlobalData = GetSingleton<GlobalData>(),
            BasicSharedData = GetSingleton<BasicBallSharedData>(),
            Rand = Random.Range(1, 361)
        }; //.ScheduleParallel();
        Dependency = ballSetupJob.ScheduleParallel(Dependency);
        World.GetOrCreateSystem<EntityCommandBufferSystem>().AddJobHandleForProducer(Dependency);
        Dependency.Complete();

        ballTypes.Dispose();
        numToSpawn.Dispose();
    }
    
    [BurstCompile]
    private partial struct BallSetupJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter cmBuffer;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public GlobalData GlobalData;
        [ReadOnly] public BasicBallSharedData BasicSharedData;
        [ReadOnly] public int Rand;
        
        [NativeSetThreadIndex]
        private int m_ThreadIndex;
        
        [BurstCompile]
        void Execute(Entity entity, ref PhysicsVelocity pv, in NewBallTag nbTag, in BallTag ballTag)
        {
            float randomAngle = math.radians(Rand);
            int speed = 1;
            switch (ballTag.Type)
            {
                case BallType.BasicBall:
                    speed = BasicSharedData.Speed;
                    break;
            }
            
            pv.Linear = new float3(math.cos(randomAngle) * speed * GlobalData.GlobalSpeed * GlobalData.SpeedScale * DeltaTime, 
                math.sin(randomAngle) * speed * GlobalData.GlobalSpeed * GlobalData.SpeedScale * DeltaTime, 0);
            
            cmBuffer.RemoveComponent<NewBallTag>(m_ThreadIndex, entity);
        }
    }
}

public enum BallType
{
    BasicBall
}
