using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class TrapFloorSpikes : MonoBehaviour
{
    [Tooltip("How long should it be paused after moving?")]
    [SerializeField] private float PauseTimeInSeconds = 1;

    [Tooltip("Speed the spikes go up or down")]
    [SerializeField] private float Speed = 2f;

    private bool IsMoving = false;
    private bool Direction = false;
    private float PauseTimeRemaining = -1f;

    private float MaxY = 0;
    private float MinY = 0;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 currentPos = this.transform.Find("Floor").transform.localPosition;
        MinY = currentPos.y - 1.20f;
        MaxY = currentPos.y;

        PauseTimeRemaining -= Time.deltaTime;
        if (PauseTimeRemaining < 0)
        {
            IsMoving = true;
        }

        // We can stop now.
        if (IsMoving)
        {
            Vector3 position = this.transform.Find("Spikes").transform.localPosition;
            if (Direction)
            {
                position += new Vector3(0, Speed * Time.deltaTime * 12f, 0);
                if (position.y > MaxY)
                {
                    IsMoving = false;
                    Direction = !Direction;
                    PauseTimeRemaining = PauseTimeInSeconds;
                }
            }
            else
            {
                position -= new Vector3(0, Speed * Time.deltaTime * 12f, 0);
                if (position.y < MinY)
                {
                    IsMoving = false;
                    Direction = !Direction;
                    PauseTimeRemaining = PauseTimeInSeconds;
                }
            }

            this.transform.Find("Spikes").transform.localPosition = position;
        }
    }
}
