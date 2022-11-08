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

    private NativeList<int> _collisionEventIds;

    [BurstCompile]
    protected override void OnCreate()
    {
        base.OnCreate();
        _collisionEventIds = new NativeList<int>(Allocator.Persistent);
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
        //this.RegisterPhysicsRuntimeSystemReadWrite();
    }

    [BurstCompile]
    protected override void OnDestroy()
    {
        base.OnDestroy();
        _collisionEventIds.Dispose();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        var ecb = _endSimECBSystem.CreateCommandBuffer();

        NativeQueue<GlobalDataEventArgs>.ParallelWriter globalDataEventQueueParallelWriter =
            World.GetOrCreateSystem<GlobalDataUpdateSystem>().EventQueue.AsParallelWriter();
        NativeQueue<int>.ParallelWriter levelOverEventQueueParallelWriter =
            World.GetOrCreateSystem<LevelControlSystem>().EventQueue.AsParallelWriter();
        NativeQueue<SpawnEventArgs>.ParallelWriter spawnEventQueueParallelWriter =
            World.GetOrCreateSystem<BallSpawnSystem>().SpawnBallEventQueue.AsParallelWriter();
        
        new CannonballPhysicsJob
        {
            Manager = _entityManager,
            GlobalData = GetSingleton<GlobalData>(),
            CommandBuffer = ecb,
            DeltaTime = Time.DeltaTime,
            Random = new Random((uint)DateTime.Now.Ticks),
            Bricks = _brickQuery.ToEntityArray(Allocator.TempJob),
            Translations = GetComponentDataFromEntity<Translation>(),
            Velocities = GetComponentDataFromEntity<PhysicsVelocity>(),
            BallDatas = GetComponentDataFromEntity<BasicBallSharedData>(),
            BrickDatas = GetComponentDataFromEntity<BrickData>(),
            CollisionIds = _collisionEventIds,
            GlobalDataEventQueue = globalDataEventQueueParallelWriter,
            LevelOverEventQueue = levelOverEventQueueParallelWriter
        }.Schedule(_stepPhysicsWorldSystem.Simulation, Dependency).Complete();
        
        new CannonballSpeedJob
        {
            GlobalData = GetSingleton<GlobalData>(),
            DeltaTime = Time.DeltaTime
        }.ScheduleParallel(Dependency).Complete();

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
            ScatterData = GetComponentDataFromEntity<ScatterBallSharedData>(),
            BrickDatas = GetComponentDataFromEntity<BrickData>(),
            CollisionIds = _collisionEventIds,
            GlobalDataEventQueue = globalDataEventQueueParallelWriter,
            SpawnEventQueue = spawnEventQueueParallelWriter,
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
        public ComponentDataFromEntity<ScatterBallSharedData> ScatterData;
        public ComponentDataFromEntity<BrickData> BrickDatas;

        public NativeList<int> CollisionIds;
        public NativeQueue<GlobalDataEventArgs>.ParallelWriter GlobalDataEventQueue;
        public NativeQueue<SpawnEventArgs>.ParallelWriter SpawnEventQueue;

        public NativeQueue<int>.ParallelWriter LevelOverEventQueue;

        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            if (!Manager.HasComponent<DoneSetupTag>(collisionEvent.EntityA) || !Manager.HasComponent<DoneSetupTag>(collisionEvent.EntityB)) return;

            Entity ballEntity = (Manager.HasComponent<BasicBallSharedData>(collisionEvent.EntityB))
                ? collisionEvent.EntityB
                : collisionEvent.EntityA;
            BasicBallSharedData ballData = BallDatas[ballEntity];

            if (Manager.HasComponent<WallTag>(collisionEvent.EntityA) ||
                Manager.HasComponent<WallTag>(collisionEvent.EntityB))
            {
                //Debug.Log("hit wall!");
                if (!Manager.HasComponent<WallColliderTag>(ballEntity) || Bricks.Length == 0)
                {
                    if (Manager.HasComponent<CannonballTag>(ballEntity))
                    {
                        //Debug.Log("Cannonball hit wall!");
                        PhysicsVelocity inVel = Velocities[ballEntity];
                        Translation ballPos = Translations[ballEntity];
                        //float randOffset = Random.NextInt(-5, 6);

                        float3 reflectVel = Reflect(ballPos.Value, inVel.Linear, collisionEvent.Normal, 0f);

                        CommandBuffer.SetComponent(ballEntity,
                            new PhysicsVelocity
                            {
                                Linear = math.normalize(reflectVel) * ballData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed *
                                         DeltaTime
                            });
                        return;
                    }

                    return;
                }

                if (Manager.HasComponent<ScatterTag>(ballEntity))
                {
                    ScatterBallSharedData scatterData = ScatterData[ballEntity];
                    SpawnEventQueue.Enqueue(new SpawnEventArgs{Type = BallType.ScatterChild, Amount = scatterData.ExtraBalls, Position = Translations[ballEntity].Value});
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
            
            if (CollisionIds.Contains(brickEntity.Index)) return;
            
            CollisionIds.Add(brickEntity.Index);

            if (Manager.HasComponent<CannonballTag>(ballEntity))
            {
                if (brickData.Health >= ballData.Power)
                {
                    //Debug.Log("Collision event cannonball!");
                    int damage = ballData.Power * GlobalData.PowerMultiplier;
                    if (brickData.Poisoned) damage *= 2;
                    CommandBuffer.SetComponent(brickEntity, new BrickData{ Health = brickData.Health - damage, 
                        Position = brickData.Position, Poisoned = brickData.Poisoned});
                    
                    PhysicsVelocity inVel = Velocities[ballEntity];
                    Translation ballPos = Translations[ballEntity];
                    float randOffset = Random.NextInt(-5, 6);

                    float3 reflectVel = Reflect(ballPos.Value, inVel.Linear, new float3(-1, 0, 0), randOffset);
            
                    CommandBuffer.SetComponent(ballEntity, 
                        new PhysicsVelocity{ Linear = reflectVel * ballData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime });
                }
                return;
            }
            
            int dam = ballData.Power * GlobalData.PowerMultiplier;
            if (brickData.Poisoned) dam *= 2;
            int newHealth = brickData.Health - dam;
            CommandBuffer.SetComponent(brickEntity, new BrickData{ Health = newHealth, Position = brickData.Position, Poisoned = brickData.Poisoned});

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
                    
                    int damage = Mathf.Clamp(Mathf.RoundToInt(ballData.Power / 4f), 1, ballData.Power) * GlobalData.PowerMultiplier;
                    //Debug.Log(string.Format("{0}", hit.Entity.Index));
                    BrickData hitBrick = BrickDatas[hit.Entity];

                    int curHealth = hitBrick.Health;
                    if (brickData.Poisoned) damage *= 2;
                    CommandBuffer.SetComponent(brickEntity, new BrickData{ Health = brickData.Health - damage, 
                        Position = brickData.Position, Poisoned = brickData.Poisoned});
                    
                    if (curHealth - damage <= 0)
                    {
                        CommandBuffer.DestroyEntity(hit.Entity);
                        GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.BRICKS, NewData = GlobalData.Bricks - 1});
                        GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.MONEY, NewData = curHealth});
                        //LevelOverEventQueue.Enqueue(2);
                    }
                }

                hits.Dispose();
                //Debug.Log("Plasma ball hit effect!");
            }
            
            if (Manager.HasComponent<ScatterChildTag>(ballEntity))
            {
                CommandBuffer.DestroyEntity(ballEntity); 
            }

            if (newHealth <= 0)
            {
                CollisionIds.RemoveAt(CollisionIds.IndexOf(brickEntity.Index));
                CommandBuffer.DestroyEntity(brickEntity);
                GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.BRICKS, NewData = GlobalData.Bricks - 1});
                GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.MONEY, NewData = brickData.Health});
                return;
                //LevelOverEventQueue.Enqueue(2);
            }

            if (Manager.HasComponent<PoisonTag>(ballEntity) && !brickData.Poisoned)
            {
                CommandBuffer.SetComponent(brickEntity,
                    new BrickData { Health = newHealth, Position = brickData.Position, Poisoned = true });
            }

            CollisionIds.RemoveAt(CollisionIds.IndexOf(brickEntity.Index));
        }
    }

    [BurstCompile]
    private partial struct CannonballSpeedJob : IJobEntity
    {
        public GlobalData GlobalData;
        public float DeltaTime;
        
        void Execute(ref BasicBallSharedData ballSharedData, ref PhysicsVelocity pv, in CannonballTag tag)
        {
            float3 vel = pv.Linear;
            if (math.sqrt(vel.x * vel.x + vel.y * vel.y + vel.z * vel.z) < 7f)
            {
                pv.Linear = math.normalize(vel) * ballSharedData.Speed * GlobalData.SpeedScale *
                            GlobalData.GlobalSpeed * DeltaTime;
            }
        }
    }
    
    [BurstCompile]
    private struct CannonballPhysicsJob : ITriggerEventsJob
    {
        public EntityManager Manager;

        public GlobalData GlobalData;

        public EntityCommandBuffer CommandBuffer;
        
        public float DeltaTime;

        public Random Random;
        
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> Bricks;
        
        public ComponentDataFromEntity<Translation> Translations;
        public ComponentDataFromEntity<PhysicsVelocity> Velocities;
        public ComponentDataFromEntity<BasicBallSharedData> BallDatas;
        public ComponentDataFromEntity<BrickData> BrickDatas;

        public NativeList<int> CollisionIds; 

        public NativeQueue<GlobalDataEventArgs>.ParallelWriter GlobalDataEventQueue;
        public NativeQueue<int>.ParallelWriter LevelOverEventQueue;

        public void Execute(TriggerEvent triggerEvent)
        {
            if (!Manager.HasComponent<DoneSetupTag>(triggerEvent.EntityA) ||
                !Manager.HasComponent<DoneSetupTag>(triggerEvent.EntityB)) return;

            Entity ballEntity = (Manager.HasComponent<BasicBallSharedData>(triggerEvent.EntityB))
                ? triggerEvent.EntityB
                : triggerEvent.EntityA;
            BasicBallSharedData ballData = BallDatas[ballEntity];

            Entity brickEntity = (Manager.HasComponent<BrickTag>(triggerEvent.EntityB))
                ? triggerEvent.EntityB
                : triggerEvent.EntityA;
            BrickData brickData = BrickDatas[brickEntity];
            
            if (CollisionIds.Contains(brickEntity.Index)) return;
            
            CollisionIds.Add(brickEntity.Index);
            
            if (Manager.HasComponent<CannonballTag>(ballEntity))
            {
                if (brickData.Health < ballData.Power)
                {
                    /*CommandBuffer.SetComponent(ballEntity, 
                        new PhysicsVelocity{ Linear = math.normalize(Velocities[ballEntity].Linear) * ballData.Speed 
                            * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime });*/
                    //Debug.Log("Destroying brick from trigger");
                    
                    CollisionIds.RemoveAt(CollisionIds.IndexOf(brickEntity.Index));
                    CommandBuffer.DestroyEntity(brickEntity);
                    GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.BRICKS, NewData = GlobalData.Bricks - 1});
                    GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.MONEY, NewData = brickData.Health});
                    return;
                    //LevelOverEventQueue.Enqueue(2);
                }
            }
            
            CollisionIds.RemoveAt(CollisionIds.IndexOf(brickEntity.Index));
        }
    }
}


