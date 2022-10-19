using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
[Serializable]
public struct PrefabComponent : IComponentData
{
    public Entity BasicBallPrefab;
    public Entity BrickPrefab;
}
