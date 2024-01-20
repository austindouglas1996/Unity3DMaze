using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Specifies the available visual themes that can be applied to mazes and rooms within the game world.
/// This enum determines the overall aesthetic and stylistic choices for the game's environments,
/// influencing the choice of prefabs and other assets used for construction.
/// </summary>
[Serializable]
public enum MazeTheme
{
    /// <summary>
    /// Represents a generic theme with no specific stylistic constraints.
    /// Whatever this object is assigned to it should be able to fit in with any theme.
    /// </summary>
    Any,

    /// <summary>
    /// This item is a child of another object and instead the parent should be found and the theme should be located there.
    /// </summary>
    UseParent,

    /// <summary>
    /// Default option built for me.
    /// </summary>
    Castle
}