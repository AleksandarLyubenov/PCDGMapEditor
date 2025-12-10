using System;
using UnityEngine;

[Serializable]
public class UnitData
{
    public string id;            // unique GUID for this unit
    public Vector2 worldPos;
    public string frameType;     // "LAND", "SEA", "SUB", "AIR"
    public string unitCode;      // e.g. "MI", "DDG", "F18"
    public string nationTop;     // text above frame (optional)
    public string labelBottom;   // text below frame (optional)
    [Range(0, 255)] public byte importance;
}
