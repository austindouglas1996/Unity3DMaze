
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum PropSize
{
    [Tooltip("Allow any prop size.")]
    Any,

    [Tooltip("Super Small. Does not take almost any space. A second prop could be added without issue.")]
    SuperSmall,

    [Tooltip("Small. Does not have a chance to block doorways.")]
    Small,

    [Tooltip("Medium. May be large enough to block doorways or roofs.")]
    Medium,

    [Tooltip("Large. Guaranteed to take up the entire tile. Will block doorways.")]
    Large,

    [Tooltip("Takes up multiple tiles used as a main room piece.")]
    Special
}


public class PropMono : MonoBehaviour
{
    [Tooltip("Cateogory for this item. Use 'Any' if it fits all.")]
    [SerializeField] public MazeTheme Theme = MazeTheme.Any;

    [Tooltip("Cateogory for this item. Use 'Any' if it fits all.")]
    [SerializeField] public RoomFixtureIdentityType Type = RoomFixtureIdentityType.Any;

    [Tooltip("Size of this item.")]
    [SerializeField] public PropSize Size;

    [Tooltip("Does this prop require the parent prop it's attached to be set on the floor level.")]
    [SerializeField] public bool RequiresFloor = false;
}
