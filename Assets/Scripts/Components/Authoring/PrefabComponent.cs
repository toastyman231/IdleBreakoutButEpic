using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

[GenerateAuthoringComponent]
[Serializable]
public struct PrefabComponent : IComponentData
{
    public Entity BasicBallPrefab;
    public Entity PlasmaBallPrefab;
    public Entity SniperBallPrefab;
    public Entity ScatterBallPrefab;
    public Entity ScatterChildPrefab;
    public Entity CannonballPrefab;
    public Entity PoisonBallPrefab;
    public Entity GoldBrickPrefab;
    public Entity BrickPrefab;
}
