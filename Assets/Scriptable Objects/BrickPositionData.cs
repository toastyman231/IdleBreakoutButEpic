using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Brick Layout", menuName = "Scriptable Objects/Brick Layout")]
public class BrickPositionData : ScriptableObject
{
    public float3[] positions;
}
