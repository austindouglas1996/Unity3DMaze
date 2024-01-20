using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Provides a collection of resources used for generic room building.
/// </summary>
public sealed class MazeResourceManager : MonoBehaviour
{
    [Header("Styles")]
    [SerializeField] private List<RoomThemePrefabs> Themes = new List<RoomThemePrefabs>();

    [Header("Modifers")]
    [SerializeField] public RoomSpawnOptions Chances = new RoomSpawnOptions();

    [Header("Debug")]
    [SerializeField] public GameObject DebugCube;

    /// <summary>
    /// Get the one instance of this class.
    /// </summary>
    public static MazeResourceManager Instance { get; private set; }
    private static MazeResourceManager _Instance;

    /// <summary>
    /// Gets the Castle theme.
    /// </summary>
    public RoomThemePrefabs Castle
    {
        get { return GetTheme(MazeTheme.Castle); }
    }

    /// <summary>
    /// Gets the Castle theme.
    /// </summary>
    public RoomThemePrefabs Default
    {
        get { return GetTheme(MazeTheme.Castle); }
    }

    /// <summary>
    /// Returns a theme based on <see cref="MazeTheme"/>.
    /// </summary>
    /// <param name="theme"></param>
    /// <returns></returns>
    public RoomThemePrefabs GetTheme(MazeTheme theme)
    {
        return Themes.FirstOrDefault(t => t.Theme == theme);
    }

    /// <summary>
    /// Returns <see cref="Instance"/>
    /// </summary>
    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }
}
