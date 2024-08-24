using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class PocketableItem : InteractiveItemBase
{
    [Tooltip("Does this item require both players hands when being held?")]
    [SerializeField] public bool RequiresTwoHands = false;

    [Tooltip("Does this item slow down the player and not allow them to make jumps easily?")]
    [SerializeField] public bool ExtraHeavy = false;

    [Tooltip("Can the player throw the item?")]
    [SerializeField] public bool CanBeThrown = false;

    [Tooltip("Can the player hand the item to another player?")]
    [SerializeField] public bool CanBeGifted = false;

    [Tooltip("Can the player hand the item to another player?")]
    [SerializeField] public bool IsWeapon = false;

    [Tooltip("Can the player hand the item to another player?")]
    [SerializeField] public bool IsTrigger = false;

    [Tooltip("As this item been recently gifted?")]
    [SerializeField] public bool IsRecentlyGifted = false;

    [Tooltip("Can be sold.")]
    [SerializeField] public bool IsSellable = false;

    [Tooltip("Minimum sell price.")]
    [SerializeField] private int MinSellPrice = 0;

    [Tooltip("Maximum sell price.")]
    [SerializeField] private int MaxSellPrice = 0;

    [Tooltip("How rare the item is. 1 is default and does affect pricing.")]
    [SerializeField] private float Rarity = 1;

    [Tooltip("The position to adjust the item position when in camera view.")]
    [SerializeField] public Vector3 OffsetPosition = Vector3.zero;

    [Tooltip("The rotation to set the item position when in camera view.")]
    [SerializeField] public Quaternion OffsetRotation = Quaternion.identity;

    [Tooltip("The scale to set the item position when in camera view.")]
    [SerializeField] public Vector3 OffsetScale = Vector3.zero;

    [Tooltip("The sound played when the item is first picked up."), XmlIgnore]
    [SerializeField] public AudioSource PickupSound;
    public string PickupSoundName { get; set; }

    [Tooltip("The sound played when the item is dropped and collides with an object."), XmlIgnore]
    [SerializeField] public AudioSource DropSound;
    public string DropSoundName { get; set; }

    [Tooltip("The sound played when the player moves with the object."), XmlIgnore]
    [SerializeField] public AudioSource MoveSound;
    public string MoveSoundName { get; set; }

    [Tooltip("The sound played when the player presses the trigger button."), XmlIgnore]
    [SerializeField] public AudioSource ActionSound;
    public string ActionSoundName { get; set; }

    /// <summary>
    /// Returns whether <see cref="Generate"/> has been called.
    /// </summary>
    public bool GenerateCalled { get; private set; }

    /// <summary>
    /// Returns whether <see cref="Generate"/> has finished.
    /// </summary>
    public bool GenerateFinished { get; private set; }

    /// <summary>
    /// Gets the value of the item that it should be sold for.
    /// </summary>
    public int Value
    {
        get { return _Value; }
    }
    private int _Value = 0;

    /// <summary>
    /// Called, yada yada yada.
    /// </summary>
    private void Start()
    {
        if (this.GenerateCalled) return;
        GenerateCalled = true;

        _Value = (int)(Random.Range(MinSellPrice, MaxSellPrice) * Rarity);

        GenerateFinished = true;
    }
}