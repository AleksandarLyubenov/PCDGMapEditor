using System.Collections.Generic;

[System.Serializable]
public class MapSaveData
{
    public List<UnitData> units = new();
    public List<ArrowData> arrows = new();
    public string backgroundRelativePath;   // e.g. "Maps/map01.png"
}
