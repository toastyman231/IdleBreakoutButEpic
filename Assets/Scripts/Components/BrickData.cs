using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable, GenerateAuthoringComponent]
public struct BrickData : IComponentData
{
    public int Health;
    public float3 Position;
    public bool Poisoned;
}
