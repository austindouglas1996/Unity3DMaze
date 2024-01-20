using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This class only exists because Unity does not support using <see cref="Dictionary"/>.
/// </summary>
[System.Serializable]
public class PropSizeChance
{
    public int Chance = 40;
    public PropSize Size;
}

/// <summary>
/// This class only exists because Unity does not support using <see cref="Dictionary"/>.
/// </summary>
[System.Serializable]
public class PropTypeChance
{
    public int Chance = 40;
    public RoomFixtureIdentityType Type;
}

/// <summary>
/// Represents a group of options for rooms and how their spawns work.
/// </summary>
[System.Serializable]
public class RoomSpawnOptions
{
    [Tooltip("Chance a trap will be spawned on a tile in percent.")]
    [SerializeField] public int ChanceForTrap = 40;

    [Tooltip("Chance a special tile will generate a special prop instead of a generic item.")]
    [SerializeField] public int ChanceForFloorSpecial = 80;

    [Header("Spawn chances")]
    [SerializeField]
    private List<PropTypeChance> SpawnChances = new List<PropTypeChance>()
    {
        new PropTypeChance() { Chance = 100, Type = RoomFixtureIdentityType.Door},
        new PropTypeChance() { Chance = 80, Type = RoomFixtureIdentityType.Wall},
        new PropTypeChance() { Chance = 80, Type = RoomFixtureIdentityType.ShortWall},
        new PropTypeChance() { Chance = 70, Type = RoomFixtureIdentityType.Floor},
        new PropTypeChance() { Chance = 80, Type = RoomFixtureIdentityType.Window},
        new PropTypeChance() { Chance = 20, Type = RoomFixtureIdentityType.Roof},
    };

    [SerializeField] public List<PropSizeChance> PropSizeChances = new List<PropSizeChance>()
    {
        new PropSizeChance() { Chance = 80, Size = PropSize.SuperSmall},
        new PropSizeChance() { Chance = 40, Size = PropSize.Small},
        new PropSizeChance() { Chance = 40, Size = PropSize.Medium},
        new PropSizeChance() { Chance = 30, Size = PropSize.Large},
        new PropSizeChance() { Chance = 10, Size = PropSize.Special},
    };

    public int GetSpawnChance(RoomFixtureIdentityType type)
    {
        if (SpawnChances.FirstOrDefault(r => r.Type == type) == null)
            throw new System.ArgumentNullException("Unable to find " +type);

        return SpawnChances.FirstOrDefault(s => s.Type == type).Chance;
    }
}
