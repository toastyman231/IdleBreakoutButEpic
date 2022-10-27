using System;
using System.Numerics;
using Systems;
using Tags;
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
using Unity.VisualScripting;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//[UpdateBefore(typeof(StepPhysicsWorld))] 
public partial class BallPhysicsSystem : SystemBase
{
    private EntityManager _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    private StepPhysicsWorld _stepPhysicsWorldSystem;
    private BuildPhysicsWorld _buildPhysicsWorldSystem;
    private EndSimulationEntityCommandBufferSystem _endSimECBSystem;
    private CollisionFilter _plasmaFilter;
    private EntityQuery _brickQuery;

    // TODO: Handle all of this stuff in each Ball's UpdateSystem
    
    [BurstCompile]
    protected override void OnCreate()
    {
        base.OnCreate();
        _stepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        _buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        _endSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _brickQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<BrickTag>());
        _plasmaFilter = new CollisionFilter()
        {
            BelongsTo = 1u << 0,
            CollidesWith = 1u << 2,
            GroupIndex = 0
        };
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

        new BallPhysicsJob
        {
            Manager = _entityManager,
            GlobalData = GetSingleton<GlobalData>(),
            CommandBuffer = ecb,
            World = _buildPhysicsWorldSystem.PhysicsWorld.CollisionWorld,
            Filter = _plasmaFilter,
            DeltaTime = Time.DeltaTime,
            Random = new Random((uint)DateTime.Now.Ticks),
            Bricks = _brickQuery.ToEntityArray(Allocator.TempJob),
            Translations = GetComponentDataFromEntity<Translation>(),
            Velocities = GetComponentDataFromEntity<PhysicsVelocity>(),
            BallDatas = GetComponentDataFromEntity<BasicBallSharedData>(),
            PlasmaData = GetComponentDataFromEntity<PlasmaBallSharedData>(),
            BrickDatas = GetComponentDataFromEntity<BrickData>(),
            GlobalDataEventQueue = globalDataEventQueueParallelWriter,
            LevelOverEventQueue = levelOverEventQueueParallelWriter
        }.Schedule(_stepPhysicsWorldSystem.Simulation, Dependency).Complete();
        //_endSimECBSystem.AddJobHandleForProducer(basicJob);
        
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

    private static float3 Reflect(float3 pos, float3 inVel, float3 normal, float offset)
    {
        float3 incomingVelocity = pos - inVel;
        normal = math.normalize(normal);
        float3 reflectedVelocity = incomingVelocity - 2 * math.dot(incomingVelocity, normal) * normal;
        reflectedVelocity += offset;
        reflectedVelocity.z = 0;

        return reflectedVelocity;
    }

    [BurstCompile]
    private struct BallPhysicsJob : ICollisionEventsJob
    {
        public EntityManager Manager;

        public GlobalData GlobalData;

        public EntityCommandBuffer CommandBuffer;
        
        public CollisionWorld World;

        public CollisionFilter Filter;

        public float DeltaTime;

        public Random Random;
        
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> Bricks;

        public ComponentDataFromEntity<Translation> Translations;
        public ComponentDataFromEntity<PhysicsVelocity> Velocities;
        public ComponentDataFromEntity<BasicBallSharedData> BallDatas;
        public ComponentDataFromEntity<PlasmaBallSharedData> PlasmaData;
        public ComponentDataFromEntity<BrickData> BrickDatas;

        public NativeQueue<GlobalDataEventArgs>.ParallelWriter GlobalDataEventQueue;
        
        public NativeQueue<int>.ParallelWriter LevelOverEventQueue;

        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity ballEntity = (Manager.HasComponent<BasicBallSharedData>(collisionEvent.EntityB))
                ? collisionEvent.EntityB
                : collisionEvent.EntityA;
            BasicBallSharedData ballData = BallDatas[ballEntity];

            if (Manager.HasComponent<WallTag>(collisionEvent.EntityA) ||
                Manager.HasComponent<WallTag>(collisionEvent.EntityB))
            {
                if (!Manager.HasComponent<SniperTag>(ballEntity) || Bricks.Length == 0)
                {
                    /*PhysicsVelocity vel = Velocities[ballEntity];
                    Translation tr = Translations[ballEntity];
                    float rand = Random.NextInt(-5, 6);

                    float3 refVel = Reflect(tr.Value, vel.Linear, collisionEvent.Normal, rand);
            
                    CommandBuffer.SetComponent(ballEntity, 
                        new PhysicsVelocity{ Linear = refVel * ballData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime });*/
                    
                    return;
                }

                Translation trans = Translations[ballEntity];

                float distance = float.MaxValue;
                float3 closestBrickPos = float3.zero;

                foreach (var brick in Bricks)
                {
                    BrickData closestBrickData = BrickDatas[brick];
                    
                    float dist = math.distance(trans.Value, closestBrickData.Position);
                    if (dist <= distance)
                    {
                        distance = dist;
                        closestBrickPos = closestBrickData.Position;
                    }
                }

                float3 vectorToTarget = math.normalize(closestBrickPos - trans.Value);
                vectorToTarget = math.normalize(new float3(vectorToTarget.x, vectorToTarget.y, 0));
                //Debug.Log(string.Format("Unaltered: {0}", vectorToTarget));
                CommandBuffer.SetComponent(ballEntity, 
                    new PhysicsVelocity{ Linear = vectorToTarget * ballData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime });
                //Debug.Log(string.Format("After hit: {0}", vectorToTarget * ballData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime));
                return;
            }
            
            Entity brickEntity = (Manager.HasComponent<BrickTag>(collisionEvent.EntityB))
                ? collisionEvent.EntityB
                : collisionEvent.EntityA;

            BrickData brickData = BrickDatas[brickEntity];

            int health = brickData.Health;
            int newHealth = health - ballData.Power * GlobalData.PowerMultiplier;
            CommandBuffer.SetComponent(brickEntity, new BrickData{ Health = newHealth, Position = brickData.Position });

            if (Manager.HasComponent<PlasmaTag>(ballEntity))
            {
                float3 pos = brickData.Position;
                PlasmaBallSharedData plasmaData = PlasmaData[ballEntity];
                
                NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
                
                // Might want to expand the range scale into a global var
                World.OverlapSphere(pos, plasmaData.Range * 1.5f, ref hits, Filter);

                foreach (var hit in hits)
                {
                    if (hit.Distance <= 0.1f) continue;
                    
                    int damage = Mathf.Clamp(Mathf.RoundToInt(ballData.Power / 4f), 1, ballData.Power);
                    //Debug.Log(string.Format("{0}", hit.Entity.Index));
                    BrickData hitBrick = BrickDatas[hit.Entity];

                    int curHealth = hitBrick.Health;
                    int brickHealth = curHealth - damage * GlobalData.PowerMultiplier;
                    CommandBuffer.SetComponent(hit.Entity, new BrickData{ Health = brickHealth, Position = brickData.Position });
                    
                    if (newHealth <= 0)
                    {
                        CommandBuffer.DestroyEntity(hit.Entity);
                        GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.BRICKS, NewData = GlobalData.Bricks - 1});
                        GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.MONEY, NewData = curHealth});
                        LevelOverEventQueue.Enqueue(2);
                    }
                }

                hits.Dispose();
                //Debug.Log("Plasma ball hit effect!");
            }

            if (newHealth <= 0)
            {
                CommandBuffer.DestroyEntity(brickEntity);
                GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.BRICKS, NewData = GlobalData.Bricks - 1});
                GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.MONEY, NewData = health});
                LevelOverEventQueue.Enqueue(2);
            }

            //Bricks.Dispose();
            
            /*PhysicsVelocity inVel = Velocities[ballEntity];
            Translation ballPos = Translations[ballEntity];
            float randOffset = Random.NextInt(-5, 6);

            float3 reflectVel = Reflect(ballPos.Value, inVel.Linear, collisionEvent.Normal, randOffset);
            
            CommandBuffer.SetComponent(ballEntity, 
                new PhysicsVelocity{ Linear = reflectVel * ballData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime });*/

            //Debug.DrawRay(TranslationComponentData[collisionEvent.EntityA].Value, math.normalize(incomingVelocity), Color.yellow);
            //Debug.DrawRay(collisionEvent.CalculateDetails(ref MyPhysicsWorld).AverageContactPointPosition, normal, Color.blue);
            //Debug.DrawRay(TranslationComponentData[collisionEvent.EntityA].Value, math.normalize(reflectedVelocity), Color.green);

            //pv.Linear = reflectedVelocity * BallSharedData.Speed;
        }
    }
}


