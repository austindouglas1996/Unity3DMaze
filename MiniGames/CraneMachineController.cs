using OverfortGames.FirstPersonController;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using VHierarchy.Libs;

public class CraneMachineController : MonoBehaviour
{
    /// <summary>
    /// This way we can stop player.
    /// </summary>
    [SerializeField] private GameObject Player;

    /// <summary>
    /// The object that controls the claws position.
    /// </summary>
    [SerializeField] private GameObject Arm;

    /// <summary>
    /// Holds the claw steady.
    /// </summary>
    [SerializeField] private GameObject Hand;

    /// <summary>
    /// Extends the claw up/down into the machine.
    /// </summary>
    [SerializeField] private GameObject Extender;

    /// <summary>
    /// The attachment that holds the item.
    /// </summary>
    [SerializeField] private GameObject Claw;

    /// <summary>
    /// The dropoff point to drop objects to.
    /// </summary>
    [SerializeField] private GameObject Dropper;

    /// <summary>
    /// Tells whether we're accepting input currently.
    /// </summary>
    [SerializeField] private bool AcceptingInput = false;

    [SerializeField] private float MinExtenderHeight = 9f;
    [SerializeField] private float MaxExtenderHeight = 85f;

    [SerializeField] private Vector3 StartPositionOfClaw;
    [SerializeField] private Vector3 EndPositionOfClaw;

    [SerializeField] private float SpeedArmMoves = 0.1f;
    [SerializeField] private float SpeedClawMoves = 0.3f;
    [SerializeField] private float SpeedClawRotates = 0.3f;
    [SerializeField] private float DefaultChanceToDrop = 1f;

    private bool RequiresReset = false;
    private bool RequiresLift = false;
    private bool IsGrabbing = false;
    private bool IsMoving = false;
    private bool IsResetting = false;
    public bool GrabbedItem = false;

    private PocketableItem HoldingItem;
    public EntityItemInventory Inventory;
    private EntityInventoryTrigger InventoryTrigger;

    public void MoveArmRight()
    {
        this.Arm.transform.position -= new Vector3(SpeedArmMoves,0,0);
    }

    public void MoveArmLeft()
    {
        this.Arm.transform.position += new Vector3(SpeedArmMoves, 0, 0);
    }

    public void MoveHandUp()
    {
        this.Hand.transform.position -= new Vector3(0,0,SpeedArmMoves);
        this.Claw.transform.position -= new Vector3(0, 0, SpeedArmMoves);
    }

    public void MoveHandDown()
    {
        this.Hand.transform.position += new Vector3(0, 0, SpeedArmMoves);
        this.Claw.transform.position += new Vector3(0, 0, SpeedArmMoves);
    }

    public void StartGrab()
    {
        StartCoroutine(LowerExtender());
    }

    private IEnumerator LowerExtender()
    {
        float StartZPosition = Extender.transform.position.z;

        while (Claw.transform.localPosition.y > 1.5f)
        {
            // Lower the claw along with the extender
            Claw.transform.position -= new Vector3(0, Time.deltaTime * SpeedClawMoves, 0);

            if (GrabbedItem)
            {
                this.RequiresLift = true;
                Inventory.SelectedItem.GetComponent<Rigidbody>().useGravity = false;
                Inventory.SelectedItem.transform.parent = this.Claw.transform;
                Inventory.SelectedItem.transform.position = this.Claw.transform.position - new Vector3(0,0.7f,0);
                yield break;
            }

            yield return null;
        }

        this.RequiresLift = true;
        yield break;
    }

    private IEnumerator RaiseExtender()
    {
        while (Claw.transform.position.y < StartPositionOfClaw.y)
        {
            // Lower the claw along with the extender
            Claw.transform.position += new Vector3(0, Time.deltaTime * SpeedClawMoves, 0);

            yield return null;
        }

        this.IsGrabbing = false;

        if (this.Inventory.SelectedItem != null)
            this.RequiresReset = true;
    }

    private IEnumerator ResetClaw()
    {
        Vector3 targetPosition = Dropper.transform.position;

        // Move the claw in the x direction
        while (Mathf.Abs(Claw.transform.position.x - targetPosition.x) > 0.01f)
        {
            Claw.transform.position = Vector3.MoveTowards(Claw.transform.position, new Vector3(targetPosition.x, Claw.transform.position.y, Claw.transform.position.z), Time.deltaTime * 0.6f);
            this.Arm.transform.position = Vector3.MoveTowards(Claw.transform.position, new Vector3(targetPosition.x, Claw.transform.position.y, Claw.transform.position.z), Time.deltaTime * 0.6f);
            this.Hand.transform.position = Vector3.MoveTowards(Claw.transform.position, new Vector3(targetPosition.x, Claw.transform.position.y, Claw.transform.position.z), Time.deltaTime * 0.6f);
            yield return null;
        }

        // Move the claw in the z direction
        while (Mathf.Abs(Claw.transform.position.z - targetPosition.z) > 0.01f)
        {
            Claw.transform.position = Vector3.MoveTowards(Claw.transform.position, new Vector3(Claw.transform.position.x, Claw.transform.position.y, targetPosition.z), Time.deltaTime * 0.6f);
            this.Hand.transform.position = Vector3.MoveTowards(Claw.transform.position, new Vector3(Claw.transform.position.x, Claw.transform.position.y, targetPosition.z), Time.deltaTime * 0.6f);
            yield return null;
        }

        // Drop the item
        Inventory.SelectedItem.GetComponent<Rigidbody>().useGravity = true;
        Inventory.SelectedItem.transform.parent = this.transform.parent;
        Inventory.SelectedItem.transform.position = new Vector3(100, 100, 100);
        Inventory.Drop(0);

        this.GrabbedItem = false;
    }


    private void Start()
    {
        this.Inventory = this.GetComponent<EntityItemInventory>();
        this.InventoryTrigger = this.GetComponentInChildren<EntityInventoryTrigger>();
        this.StartPositionOfClaw = this.Claw.transform.position;
    }

    private void Update()
    {
        if (this.RequiresLift)
        {
            StartCoroutine(this.RaiseExtender());
            RequiresLift = false;
        }

        if (this.RequiresReset)
        {
            StartCoroutine(this.ResetClaw());
            this.RequiresReset = false;
        }

        if (Input.GetKey(KeyCode.F1))
        {
            MoveArmRight();
        }

        if (Input.GetKey(KeyCode.F2))
        {
            MoveArmLeft();
        }

        if (Input.GetKey(KeyCode.F3))
        {
            MoveHandUp();
        }

        if (Input.GetKey(KeyCode.F4))
        {
            MoveHandDown();
        }

        if (Input.GetKey(KeyCode.KeypadEnter) && !this.IsGrabbing)
        {
            this.IsGrabbing = true;
            StartGrab();
        }
    }
}