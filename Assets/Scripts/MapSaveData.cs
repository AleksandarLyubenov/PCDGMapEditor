using System;
using System.Collections.Generic;

[Serializable]
public class MapSaveData
{
    public List<UnitData> units = new List<UnitData>();
    // Later you can add: public List<LineData> lines;
}
