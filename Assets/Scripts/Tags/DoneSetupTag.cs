using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable, BurstCompile]
public struct DoneSetupTag : IComponentData
{
}
