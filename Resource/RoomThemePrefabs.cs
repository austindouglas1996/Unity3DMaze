using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Provides a list of objects of similar type items to keep them consistent with one another.
/// </summary>
[System.Serializable]
public class RoomThemePrefabs
{
    [SerializeField] public MazeTheme Theme;

    [Header("Walls")]
    [SerializeField] public List<GameObject> WallsPrefabs = new List<GameObject>();
    [SerializeField] public List<GameObject> ShortWallsPrefabs = new List<GameObject>();
    [SerializeField] public List<GameObject> WallTraps = new List<GameObject>();

    [Header("Doors")]
    [SerializeField] public List<GameObject> DoorsPrefabs = new List<GameObject>();
    [SerializeField] public List<GameObject> DoorsSquarePrefabs = new List<GameObject>();
    [SerializeField] public List<GameObject> DoorsRoundPrefabs = new List<GameObject>();

    [Header("Windows")]
    [SerializeField] public List<GameObject> WindowsPrefabs = new List<GameObject>();
    [SerializeField] public List<GameObject> WindowTraps = new List<GameObject>();

    [Header("Floors")]
    [SerializeField] public List<GameObject> FloorsPrefabs = new List<GameObject>();
    [SerializeField] public List<GameObject> FloorTraps = new List<GameObject>();

    [Header("Roofs")]
    [SerializeField] public List<GameObject> RoofPrefabs = new List<GameObject>();
    [SerializeField] public List<GameObject> RoofTraps = new List<GameObject>();

    [Header("Props")]
    [Tooltip("Have a high chance to spawn a super-small prop on each structure piece.")]
    [SerializeField] public bool GenerateSuperSmallProp = true;
    [SerializeField] private List<PropMono> Props = new List<PropMono>();

    public List<PropMono> GetProp(RoomFixtureIdentityType type)
    {
        try
        {
            return this.Props.Where(r => r.Type == type).ToList();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to find any props for " + type.ToSafeString());
        }

        return null;
    }
}
