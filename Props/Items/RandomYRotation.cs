using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomYRotation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.transform.rotation = Quaternion.Euler(this.transform.rotation.x, Random.Range(-360, 360), this.transform.rotation.z);
    }
}
