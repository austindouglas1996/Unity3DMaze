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
    [SerializeField] public List<GameObject> DoorsTraps = new List<GameObject>();

    [Header("Windows")]
    [SerializeField] public List<GameObject> WindowsPrefabs = new List<GameObject>();
    [SerializeField] public List<GameObject> WindowTraps = new List<GameObject>();

    [Header("Floors")]
    [SerializeField] public List<GameObject> FloorsPrefabs = new List<GameObject>();
    [SerializeField] public List<GameObject> FloorTraps = new List<GameObject>();

    [Header("Roofs")]
    [SerializeField] public List<GameObject> RoofPrefabs = new List<GameObject>();
    [SerializeField] public List<GameObject> RoofTraps = new List<GameObject>();
}
