using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

[AlwaysUpdateSystem, BurstCompile]
public partial class BrickClickSystem : SystemBase
{
    public bool CanClick;
    
    private Camera _mainCamera;
    private BuildPhysicsWorld _buildPhysicsWorld;
    private CollisionWorld _collisionWorld;
    private GlobalData _globalData;
    private NativeQueue<GlobalDataEventArgs>.ParallelWriter _globalDataEventQueue;
    private NativeQueue<int>.ParallelWriter _levelOverEventQueue;
    private NativeQueue<TextEventData>.ParallelWriter _textEventQueue;

    [BurstCompile]
    protected override void OnCreate()
    {
        base.OnCreate();
        CanClick = true;
        //_mainCamera = Camera.main;
        _buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
    }
    
    [BurstCompile]
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        _globalData = GetSingleton<GlobalData>();
        _mainCamera = Camera.main;
    }
    
    [BurstCompile]
    protected override void OnUpdate()
    {
        //Debug.Log("Mouse pos: " + _mainCamera.ViewportToWorldPoint(Input.mousePosition));
        if (Input.GetMouseButtonUp(0))
        {
            Click();
        }
    }
    
    [BurstCompile]
    private void Click()
    {
        if (!CanClick) return;
        
        _collisionWorld = _buildPhysicsWorld.PhysicsWorld.CollisionWorld;
        _globalData = GetSingleton<GlobalData>();
        _globalDataEventQueue = World.GetOrCreateSystem<GlobalDataUpdateSystem>().EventQueue.AsParallelWriter();
        _levelOverEventQueue = World.GetOrCreateSystem<LevelControlSystem>().EventQueue.AsParallelWriter();
        _textEventQueue = BrickTextControl.Instance.EventQueue.AsParallelWriter();

        var ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        var rayStart = ray.origin;
        var rayEnd = ray.GetPoint(100f);

        if (Raycast(rayStart, rayEnd, out var raycastHit))
        {
            //Debug.Log("Location clicked: " + raycastHit.Position);
            var hitEntity = _buildPhysicsWorld.PhysicsWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
            if (EntityManager.HasComponent<ClickableTag>(hitEntity) && EntityManager.HasComponent<BrickTag>(hitEntity))
            {
                var brickData = EntityManager.GetComponentData<BrickData>(hitEntity);
                //Debug.Log("Brick location: " + brickData.Position);
                //Debug.Log("Entity: " + hitEntity.Index);
                int damage = _globalData.ClickX * _globalData.PowerMultiplier;
                if (brickData.Poisoned) damage *= 2;
                int newHealth = brickData.Health - damage;
                EntityManager.SetComponentData<BrickData>(hitEntity, new BrickData{Health = newHealth, Position = brickData.Position, Poisoned = brickData.Poisoned});
                
                _textEventQueue.Enqueue(new TextEventData{Delete = false, Update = true, Position = brickData.Position, Text = newHealth});
                
                if (newHealth <= 0)
                {
                    _textEventQueue.Enqueue(new TextEventData{Delete = true, Update = false, Position = brickData.Position, Text = newHealth});
                    EntityManager.DestroyEntity(hitEntity);

                    if (_globalData.CurrentLevel % 20 == 0)
                    {
                        World.GetOrCreateSystem<MoneySystem>().GoldToClaim +=
                            World.GetOrCreateSystem<MoneySystem>().GoldStep;
                    }
                    
                    _globalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.BRICKS, NewData = _globalData.Bricks - 1});
                    _globalDataEventQueue.Enqueue(new GlobalDataEventArgs{EventType = Field.MONEY, NewData = brickData.Health});
                    //_levelOverEventQueue.Enqueue(1);
                }
            }
        }
    }
    
    [BurstCompile]
    private bool Raycast(float3 rayStart, float3 rayEnd, out RaycastHit raycastHit)
    {
        var raycastInput = new RaycastInput
        {
            Start = rayStart,
            End = rayEnd,
            Filter = new CollisionFilter
            {
                BelongsTo = (uint) CollisionLayers.Click,
                CollidesWith = (uint) CollisionLayers.Brick
            }
        };
        return _collisionWorld.CastRay(raycastInput, out raycastHit);
    }
}
