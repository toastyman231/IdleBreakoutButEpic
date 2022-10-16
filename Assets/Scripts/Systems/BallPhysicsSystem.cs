using Unity.Burst;
using Unity.Collections;
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
    private Random _rand = new Random(100);
    private BuildPhysicsWorld _buildPhysicsWorldSystem;
    private StepPhysicsWorld _stepPhysicsWorldSystem;
    
    // TODO: Handle all of this stuff in each Ball's UpdateSystem
    
    [BurstCompile]
    protected override void OnCreate()
    {
        base.OnCreate();
        _stepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        _buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }
    
    [BurstCompile]
    protected override void OnStartRunning()
    {
        /*var ballSetupJob = new BallSetupJob
        {
            DeltaTime = Time.DeltaTime,
            GlobalData = GetSingleton<GlobalData>(),
            BasicSharedData = GetSingleton<BasicBallSharedData>(),
            Rand = _rand
            
        }.ScheduleParallel();
        ballSetupJob.Complete();*/
        this.RegisterPhysicsRuntimeSystemReadWrite();
    }
    
    [BurstCompile]
    protected override void OnUpdate()
    {
        /*Dependency = new BallPhysicsJob
        {
            VelocityComponentData = GetComponentDataFromEntity<PhysicsVelocity>(),
            TranslationComponentData = GetComponentDataFromEntity<Translation>(),
            SpeedComponentData = GetComponentDataFromEntity<Speed>(),
            //MyPhysicsWorld = _buildPhysicsWorldSystem.PhysicsWorld
        }.Schedule(_stepPhysicsWorldSystem.Simulation, Dependency);*/
        //var debug = new DebugDirection().ScheduleParallel();
        //physJob.Complete();
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
        public ComponentDataFromEntity<Translation> TranslationComponentData;
        public ComponentDataFromEntity<PhysicsVelocity> VelocityComponentData;
        //public ComponentDataFromEntity<Speed> SpeedComponentData;
        //public PhysicsWorld MyPhysicsWorld;
        
        public void Execute(CollisionEvent collisionEvent)
        {
            //float randomAngle = math.radians(_rand.NextInt(-1, 2));
            PhysicsVelocity pv = VelocityComponentData[collisionEvent.EntityA];

            float3 incomingVelocity = TranslationComponentData[collisionEvent.EntityA].Value - pv.Linear;
            float3 normal = math.normalize(collisionEvent.Normal);
            float3 reflectedVelocity = incomingVelocity - 2 * math.dot(incomingVelocity, normal) * normal;
            
            //Debug.DrawRay(TranslationComponentData[collisionEvent.EntityA].Value, math.normalize(incomingVelocity), Color.yellow);
            //Debug.DrawRay(collisionEvent.CalculateDetails(ref MyPhysicsWorld).AverageContactPointPosition, normal, Color.blue);
            //Debug.DrawRay(TranslationComponentData[collisionEvent.EntityA].Value, math.normalize(reflectedVelocity), Color.green);

            //pv.Linear = reflectedVelocity * SpeedComponentData[collisionEvent.EntityA].Value;
            ; /*new float3(
                math.normalize(pv.Linear).x * math.cos(randomAngle) -
                math.normalize(pv.Linear).y * math.sin(randomAngle),
                math.normalize(pv.Linear).y * math.cos(randomAngle) +
                math.normalize(pv.Linear).x * math.sin(randomAngle), 0);*/
        }
    }
}


