using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable, GenerateAuthoringComponent]
public struct PlasmaBallSharedData : IComponentData
{
    public int Range;
}