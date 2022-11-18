using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable, BurstCompile, GenerateAuthoringComponent]
public struct NormalData : IComponentData
{
    public float3 Normal;
}
