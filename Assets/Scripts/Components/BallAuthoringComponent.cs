using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
[GenerateAuthoringComponent]
public struct BallAuthoringComponent : IComponentData
{
    public Entity Prefab;
}
