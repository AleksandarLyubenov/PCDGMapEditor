using System;
using System.Collections.Generic;
using UnityEngine;

public enum UnitAffiliation
{
    Friendly,
    Hostile,
    Neutral
}

[Serializable]
public class UnitData
{
    public string id;
    public Vector2 worldPos;

    // "LAND", "SEA", "SUB", "AIR"
    public string frameType;

    public string nationTop;
    public string unitCode;
    public string labelBottom;

    public UnitAffiliation affiliation;
}

[System.Serializable]
public class ArrowData
{
    public string fromUnitId;
    public Vector2 from;
    public Vector2 to;
}