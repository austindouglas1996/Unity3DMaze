using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatuePicker : MonoBehaviour
{
    [Tooltip("A list of statues that can be picked.")]
    [SerializeField] private List<GameObject> Statues = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        Transform existingStatue = transform.Find("StatuePosition");

        GameObject chosen = Statues.Random();
        Instantiate(chosen, existingStatue.transform.position, existingStatue.transform.rotation, existingStatue.transform.parent);

        Destroy(existingStatue);
        Destroy(this.gameObject);
    }
}
