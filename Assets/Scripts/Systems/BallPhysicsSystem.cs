using System;
using Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//[UpdateBefore(typeof(StepPhysicsWorld))] 
public partial class BallPhysicsSystem : SystemBase
{
    private EntityManager _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    private StepPhysicsWorld _stepPhysicsWorldSystem;
    private EndSimulationEntityCommandBufferSystem _endSimECBSystem;
    private EntityQuery _globalDataQuery;

    // TODO: Handle all of this stuff in each Ball's UpdateSystem
    
    [BurstCompile]
    protected override void OnCreate()
    {
        base.OnCreate();
        _stepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        _endSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _globalDataQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<GlobalData>());
    }
    
    [BurstCompile]
    protected override void OnStartRunning()
    {
        this.RegisterPhysicsRuntimeSystemReadWrite();
    }
    
    [BurstCompile]
    protected override void OnUpdate()
    {
        var ecb = _endSimECBSystem.CreateCommandBuffer();

        NativeQueue<GlobalDataEventArgs>.ParallelWriter globalDataEventQueueParallelWriter =
            World.GetOrCreateSystem<GlobalDataUpdateSystem>().EventQueue.AsParallelWriter();
        NativeQueue<int>.ParallelWriter levelOverEventQueueParallelWriter =
            World.GetOrCreateSystem<LevelControlSystem>().EventQueue.AsParallelWriter();

        Dependency = new BallPhysicsJob
        {
            Manager = _entityManager,
            BallSharedData = GetSingleton<BasicBallSharedData>(),
            GlobalData = GetSingleton<GlobalData>(),
            CommandBuffer = ecb,
            GlobalDataEventQueue = globalDataEventQueueParallelWriter,
            LevelOverEventQueue = levelOverEventQueueParallelWriter
        }.Schedule(_stepPhysicsWorldSystem.Simulation, Dependency);
        _endSimECBSystem.AddJobHandleForProducer(Dependency);
        Dependency.Complete();

        //var debug = new DebugDirection().ScheduleParallel();
    }
    
    [BurstCompile]
    private partial struct DebugDirection : IJobEntity
    {
        void Execute(ref Translation translation, in PhysicsVelocity pv)
        {
            //Debug.DrawRay(translation.Value, math.normalize(pv.Linear), Color.red);
        }
    }
    
    [BurstCompile]
    private struct BallPhysicsJob : ICollisionEventsJob
    {
        public EntityManager Manager;

        public BasicBallSharedData BallSharedData;

        public GlobalData GlobalData;

        public EntityCommandBuffer CommandBuffer;

        public NativeQueue<GlobalDataEventArgs>.ParallelWriter GlobalDataEventQueue;
        
        public NativeQueue<int>.ParallelWriter LevelOverEventQueue;

        //public ComponentDataFromEntity<Translation> TranslationComponentData;
        //public ComponentDataFromEntity<PhysicsVelocity> VelocityComponentData;
        //public ComponentDataFromEntity<Speed> SpeedComponentData;
        //public PhysicsWorld MyPhysicsWorld;
        
        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            if (Manager.HasComponent<WallTag>(collisionEvent.EntityA) || Manager.HasComponent<WallTag>(collisionEvent.EntityB)) return;

            Entity brickEntity = (Manager.HasComponent<BrickTag>(collisionEvent.EntityB))
                ? collisionEvent.EntityB
                : collisionEvent.EntityA;
            
            BrickData brickData = Manager.GetComponentData<BrickData>(brickEntity);
            BasicBallSharedData sharedData = BallSharedData;
            GlobalData globalData = GlobalData;
            
            int newHealth = brickData.Health - sharedData.Power * globalData.PowerMultiplier;
            CommandBuffer.SetComponent(brickEntity, new BrickData{ Health = newHealth });

            if (newHealth <= 0)
            {
                CommandBuffer.DestroyEntity(brickEntity);
                GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.BRICKS, NewData = globalData.Bricks - 1});
                GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.MONEY, NewData = sharedData.Power});
                LevelOverEventQueue.Enqueue(2);
            }
            
            //CommandBuffer.Dispose();
            //float randomAngle = math.radians(_rand.NextInt(-1, 2));
            //PhysicsVelocity pv = VelocityComponentData[collisionEvent.EntityA];

            //float3 incomingVelocity = TranslationComponentData[collisionEvent.EntityA].Value - pv.Linear;
            //float3 normal = math.normalize(collisionEvent.Normal);
            //float3 reflectedVelocity = incomingVelocity - 2 * math.dot(incomingVelocity, normal) * normal;

            //Debug.DrawRay(TranslationComponentData[collisionEvent.EntityA].Value, math.normalize(incomingVelocity), Color.yellow);
            //Debug.DrawRay(collisionEvent.CalculateDetails(ref MyPhysicsWorld).AverageContactPointPosition, normal, Color.blue);
            //Debug.DrawRay(TranslationComponentData[collisionEvent.EntityA].Value, math.normalize(reflectedVelocity), Color.green);

            //pv.Linear = reflectedVelocity * SpeedComponentData[collisionEvent.EntityA].Value;
        }
    }
}


