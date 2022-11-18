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
using Vector3 = UnityEngine.Vector3;

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
    //private EntityCommandBuffer ecb;

    private NativeList<int> _collisionEventIds;
    private NativeList<int> _deletedBricks;

    [BurstCompile]
    protected override void OnCreate()
    {
        base.OnCreate();
        _collisionEventIds = new NativeList<int>(Allocator.Persistent);
        _deletedBricks = new NativeList<int>(Allocator.Persistent);
        _stepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        _buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        _endSimECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        //ecb = _endSimECBSystem.CreateCommandBuffer();
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
        _deletedBricks.Dispose();
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
        NativeQueue<int>.ParallelWriter updateGoldEventQueue =
            World.GetOrCreateSystem<MoneySystem>().GoldStepEventQueue.AsParallelWriter();
        NativeQueue<TextEventData>.ParallelWriter textEventQueue = BrickTextControl.Instance.EventQueue.AsParallelWriter();

        Dependency = new CannonballPhysicsJob
        {
            Manager = _entityManager,
            GlobalData = GetSingleton<GlobalData>(),
            CommandBuffer = ecb,
            DeltaTime = Time.DeltaTime,
            Random = new Random((uint)DateTime.Now.Ticks),
            Bricks = _brickQuery.ToEntityArray(Allocator.TempJob),
            Translations = GetComponentDataFromEntity<Translation>(),
            Velocities = GetComponentDataFromEntity<PhysicsVelocity>(),
            Normals = GetComponentDataFromEntity<NormalData>(),
            BallDatas = GetComponentDataFromEntity<BasicBallSharedData>(),
            BrickDatas = GetComponentDataFromEntity<BrickData>(),
            CollisionIds = _collisionEventIds,
            DeletedBricks = _deletedBricks,
            GlobalDataEventQueue = globalDataEventQueueParallelWriter,
            UpdateGoldEventQueue = updateGoldEventQueue,
            LevelOverEventQueue = levelOverEventQueueParallelWriter,
            TextEventQueue = textEventQueue
        }.Schedule(_stepPhysicsWorldSystem.Simulation, Dependency);//.Complete();
        _endSimECBSystem.AddJobHandleForProducer(Dependency);
        Dependency.Complete();
        //Dependency = cbJob;
        /*Dependency = new CannonballSpeedJob
        {
            GlobalData = GetSingleton<GlobalData>(),
            DeltaTime = Time.DeltaTime
        }.ScheduleParallel(Dependency);
        _endSimECBSystem.AddJobHandleForProducer(Dependency);
        Dependency.Complete();*/

        Dependency = new BallPhysicsJob
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
            DeletedBricks = _deletedBricks,
            GlobalDataEventQueue = globalDataEventQueueParallelWriter,
            SpawnEventQueue = spawnEventQueueParallelWriter,
            UpdateGoldEventQueue = updateGoldEventQueue,
            LevelOverEventQueue = levelOverEventQueueParallelWriter,
            TextEventQueue = textEventQueue
        }.Schedule(_stepPhysicsWorldSystem.Simulation, Dependency);//.Complete();
        _endSimECBSystem.AddJobHandleForProducer(Dependency);
        Dependency.Complete();

        new BallCorrectionJob
        {
            DeltaTime = Time.DeltaTime,
            GlobalData = GetSingleton<GlobalData>(),
            Rand = new Random((uint)DateTime.Now.Ticks)
        }.ScheduleParallel(Dependency).Complete();
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

    public void ClearDeletedBricks()
    {
        _deletedBricks.Clear();
    }

    [BurstCompile]
    private partial struct BallCorrectionJob : IJobEntity
    {
        public float DeltaTime;
        public GlobalData GlobalData;
        public Random Rand;
        
        [BurstCompile]
        public void Execute(ref BasicBallSharedData sharedData, ref PhysicsVelocity pv)
        {
            float3 newDir = math.normalize(pv.Linear);
            newDir.z = 0;
            
            if ((pv.Linear.x <= 0.01f && pv.Linear.x >= -0.01f) || (pv.Linear.y <= 0.01f && pv.Linear.y >= -0.01f))
            {
                float randomAngle = math.radians(Rand.NextInt(1, 361)); 
                pv.Linear = new float3(math.cos(randomAngle), math.sin(randomAngle), 0);
            }

            pv.Linear = newDir * sharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
        }
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
        public NativeList<int> DeletedBricks;
        public NativeQueue<GlobalDataEventArgs>.ParallelWriter GlobalDataEventQueue;
        public NativeQueue<SpawnEventArgs>.ParallelWriter SpawnEventQueue;
        public NativeQueue<int>.ParallelWriter UpdateGoldEventQueue;
        public NativeQueue<TextEventData>.ParallelWriter TextEventQueue;

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
                    /*if (Manager.HasComponent<CannonballTag>(ballEntity))
                    {
                        Debug.Log("Cannonball hit wall!");
                        PhysicsVelocity inVel = Velocities[ballEntity];
                        Translation ballPos = Translations[ballEntity];
                        //float randOffset = Random.NextInt(-5, 6);

                        float3 reflectVel = Reflect(ballPos.Value, inVel.Linear, collisionEvent.Normal, 0f);
                        reflectVel.z = 0;
                        
                        if (Velocities.HasComponent(ballEntity))
                            CommandBuffer.SetComponent(ballEntity,
                            new PhysicsVelocity
                            {
                                Linear = math.normalize(reflectVel) * ballData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed *
                                         DeltaTime
                            });
                        return;
                    }*/

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
                if (Velocities.HasComponent(ballEntity))
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

            /*if (Manager.HasComponent<CannonballTag>(ballEntity))
            {
                if (brickData.Health >= ballData.Power)
                {
                    Debug.Log("Collision event cannonball!");
                    int damage = ballData.Power * GlobalData.PowerMultiplier;
                    if (brickData.Poisoned) damage *= 2;
                    if (BrickDatas.HasComponent(brickEntity))
                        CommandBuffer.AddComponent(brickEntity, new BrickData{ Health = brickData.Health - damage, 
                        Position = brickData.Position, Poisoned = brickData.Poisoned});
                    
                    PhysicsVelocity inVel = Velocities[ballEntity];
                    Translation ballPos = Translations[ballEntity];
                    float randOffset = Random.NextInt(-5, 6);

                    float3 reflectVel = Reflect(ballPos.Value, inVel.Linear, collisionEvent.Normal, randOffset);
                    reflectVel.z = 0;
                    
                    if (Velocities.HasComponent(ballEntity))
                        CommandBuffer.SetComponent(ballEntity, 
                        new PhysicsVelocity{ Linear = reflectVel * ballData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime });
                }
                return;
            }*/

            int dam = ballData.Power * GlobalData.PowerMultiplier;
            if (brickData.Poisoned) dam *= 2;
            int newHealth = brickData.Health - dam;
            if (BrickDatas.HasComponent(brickEntity))
                CommandBuffer.AddComponent(brickEntity, new BrickData{ Health = newHealth, Position = brickData.Position, Poisoned = brickData.Poisoned});
            
            TextEventQueue.Enqueue(new TextEventData{Delete = false, Update = true, Position = brickData.Position, Text = newHealth});

            if (Manager.HasComponent<PlasmaTag>(ballEntity))
            {
                float3 pos = brickData.Position;
                PlasmaBallSharedData plasmaData = PlasmaData[ballEntity];
                
                NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
                
                // Might want to expand the range scale into a global var
                World.OverlapSphere(pos, plasmaData.Range * 1.5f, ref hits, Filter);

                foreach (var hit in hits)
                {
                    if (hit.Distance <= 0.1f || !BrickDatas.HasComponent(hit.Entity)) continue;

                    int damage = Mathf.Clamp(Mathf.RoundToInt(ballData.Power / 4f), 1, ballData.Power) * GlobalData.PowerMultiplier;
                    //Debug.Log(string.Format("{0}", hit.Entity.Index));
                    BrickData hitBrick = BrickDatas[hit.Entity];

                    int curHealth = hitBrick.Health;
                    if (brickData.Poisoned) damage *= 2;
                    CommandBuffer.AddComponent(hit.Entity, new BrickData{ Health = brickData.Health - damage, 
                        Position = brickData.Position, Poisoned = brickData.Poisoned});
                    
                    TextEventQueue.Enqueue(new TextEventData{Delete = false, Update = true, Position = hitBrick.Position, Text = brickData.Health - damage});
                    
                    if (curHealth - damage <= 0)
                    {
                        if (BrickDatas.HasComponent(hit.Entity) && !DeletedBricks.Contains(hit.Entity.Index))
                        {
                            DeletedBricks.Add(hit.Entity.Index);
                            TextEventQueue.Enqueue(new TextEventData{Delete = true, Update = false, Position = hitBrick.Position, Text = 0});
                            CommandBuffer.DestroyEntity(hit.Entity);
                        }
                        
                        if (GlobalData.CurrentLevel % 20 == 0)
                        {
                            UpdateGoldEventQueue.Enqueue(0);
                        }
                        
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
                if (Velocities.HasComponent(ballEntity))
                    CommandBuffer.DestroyEntity(ballEntity); 
            }

            if (newHealth <= 0)
            {
                CollisionIds.RemoveAt(CollisionIds.IndexOf(brickEntity.Index));
                if (BrickDatas.HasComponent(brickEntity) && !DeletedBricks.Contains(brickEntity.Index))
                {
                    DeletedBricks.Add(brickEntity.Index);
                    TextEventQueue.Enqueue(new TextEventData{Delete = true, Update = false, Position = brickData.Position, Text = 0});
                    CommandBuffer.DestroyEntity(brickEntity);
                }

                if (GlobalData.CurrentLevel % 20 == 0)
                {
                    UpdateGoldEventQueue.Enqueue(0);
                }
                
                GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.BRICKS, NewData = GlobalData.Bricks - 1});
                GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.MONEY, NewData = brickData.Health});
                return;
                //LevelOverEventQueue.Enqueue(2);
            }

            if (Manager.HasComponent<PoisonTag>(ballEntity) && !brickData.Poisoned)
            {
                if (BrickDatas.HasComponent(brickEntity))
                    CommandBuffer.AddComponent(brickEntity,
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
        
        void Execute(ref BasicBallSharedData ballSharedData, ref PhysicsVelocity pv, ref Translation translation, in CannonballTag tag)
        {
            float3 vel = pv.Linear;
            vel.z = 0;
            translation.Value.z = 10;
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
        public ComponentDataFromEntity<NormalData> Normals;
        public ComponentDataFromEntity<BasicBallSharedData> BallDatas;
        public ComponentDataFromEntity<BrickData> BrickDatas;

        public NativeList<int> CollisionIds;
        public NativeList<int> DeletedBricks;

        public NativeQueue<GlobalDataEventArgs>.ParallelWriter GlobalDataEventQueue;
        public NativeQueue<int>.ParallelWriter UpdateGoldEventQueue;
        public NativeQueue<TextEventData>.ParallelWriter TextEventQueue;
        public NativeQueue<int>.ParallelWriter LevelOverEventQueue;

        public void Execute(TriggerEvent triggerEvent)
        {
            if (!Manager.HasComponent<DoneSetupTag>(triggerEvent.EntityA) ||
                !Manager.HasComponent<DoneSetupTag>(triggerEvent.EntityB)) return;

            Entity ballEntity = (Manager.HasComponent<BasicBallSharedData>(triggerEvent.EntityB))
                ? triggerEvent.EntityB
                : triggerEvent.EntityA;
            BasicBallSharedData ballData = BallDatas[ballEntity];

            if (Manager.HasComponent<WallTag>(triggerEvent.EntityA) ||
                Manager.HasComponent<WallTag>(triggerEvent.EntityB))
            {
                Debug.Log("hit wall!");
                if (!Manager.HasComponent<WallColliderTag>(ballEntity) || Bricks.Length == 0)
                {
                    if (Manager.HasComponent<CannonballTag>(ballEntity))
                    {
                        Entity wallEntity = (Manager.HasComponent<WallTag>(triggerEvent.EntityB))
                            ? triggerEvent.EntityB
                            : triggerEvent.EntityA;
                        NormalData normal = Normals[wallEntity];
                        
                        Debug.Log("Cannonball hit wall!");
                        PhysicsVelocity inVel = Velocities[ballEntity];
                        Translation ballPos = Translations[ballEntity];
                        //float randOffset = Random.NextInt(-5, 6);

                        float3 reflectVel = Reflect(ballPos.Value, inVel.Linear, normal.Normal, 0f);
                        reflectVel.z = 0;

                        if (Velocities.HasComponent(ballEntity))
                            CommandBuffer.SetComponent(ballEntity,
                                new PhysicsVelocity
                                {
                                    Linear = math.normalize(reflectVel) * ballData.Speed * GlobalData.SpeedScale *
                                             GlobalData.GlobalSpeed *
                                             DeltaTime
                                });
                    }

                    return;
                }
            }

            Entity brickEntity = (Manager.HasComponent<BrickTag>(triggerEvent.EntityB))
                ? triggerEvent.EntityB
                : triggerEvent.EntityA;
            BrickData brickData = BrickDatas[brickEntity];
            
            if (CollisionIds.Contains(brickEntity.Index)) return;
            
            CollisionIds.Add(brickEntity.Index);
            
            if (Manager.HasComponent<CannonballTag>(ballEntity))
            {
                if (brickData.Health <= ballData.Power)
                {
                    /*CommandBuffer.SetComponent(ballEntity, 
                        new PhysicsVelocity{ Linear = math.normalize(Velocities[ballEntity].Linear) * ballData.Speed 
                            * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime });*/
                    //Debug.Log("Destroying brick from trigger");
                    
                    CollisionIds.RemoveAt(CollisionIds.IndexOf(brickEntity.Index));
                    if (BrickDatas.HasComponent(brickEntity) && !DeletedBricks.Contains(brickEntity.Index))
                    {
                        DeletedBricks.Add(brickEntity.Index);
                        TextEventQueue.Enqueue(new TextEventData{Delete = true, Update = false, Position = brickData.Position, Text = 0});
                        CommandBuffer.DestroyEntity(brickEntity);
                    }
                    
                    if (GlobalData.CurrentLevel % 20 == 0)
                    {
                        UpdateGoldEventQueue.Enqueue(0);
                    }
                    
                    GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.BRICKS, NewData = GlobalData.Bricks - 1});
                    GlobalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.MONEY, NewData = brickData.Health});
                    return;
                    //LevelOverEventQueue.Enqueue(2);
                }
                else
                {
                    int damage = ballData.Power * GlobalData.PowerMultiplier;
                    if (brickData.Poisoned) damage *= 2;
                    if (BrickDatas.HasComponent(brickEntity))
                        CommandBuffer.AddComponent(brickEntity, new BrickData{ Health = brickData.Health - damage, 
                            Position = brickData.Position, Poisoned = brickData.Poisoned});
                    TextEventQueue.Enqueue(new TextEventData{Delete = false, Update = true, Position = brickData.Position, Text = brickData.Health - damage});
                    CollisionIds.RemoveAt(CollisionIds.IndexOf(brickEntity.Index));
                    
                    PhysicsVelocity inVel = Velocities[ballEntity];
                    Translation ballPos = Translations[ballEntity];
                    //float randOffset = Random.NextInt(-5, 6);

                    float3 normal = math.normalize(Translations[ballEntity].Value - Translations[brickEntity].Value) * -1;
                    float3 reflectVel = Reflect(ballPos.Value, inVel.Linear, normal, 0f);
                    reflectVel.z = 0;

                    if (Velocities.HasComponent(ballEntity))
                        CommandBuffer.SetComponent(ballEntity,
                            new PhysicsVelocity
                            {
                                Linear = math.normalize(reflectVel) * ballData.Speed * GlobalData.SpeedScale *
                                         GlobalData.GlobalSpeed *
                                         DeltaTime
                            });
                }
            }
        }
    }
}


