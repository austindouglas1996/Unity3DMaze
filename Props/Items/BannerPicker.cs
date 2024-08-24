using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BannerPicker : MonoBehaviour
{
    [Tooltip("The objects of banners you'd like to use.")]
    [SerializeField] private List<GameObject> Banners = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        GameObject chosenBanner = Banners.Random();
        Instantiate(chosenBanner, this.transform.position, this.transform.rotation, this.transform.parent);
        Destroy(this.gameObject);
    }
}
