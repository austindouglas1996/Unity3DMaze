using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPaintingPicker : MonoBehaviour
{
    [Tooltip("Frames to use for the picture.")]
    [SerializeField] private List<GameObject> Frames = new List<GameObject>();

    [Tooltip("Picture to use with the picture.")]
    [SerializeField] private List<GameObject> Pictures = new List<GameObject>();

    private void Start()
    {
        Transform existingFrame = transform.Find("Frame");
        Transform existingPicture = transform.Find("Picture");

        GameObject selectedFrame = Frames.Random();
        GameObject selectedPicture = Pictures.Random();

        GameObject newFrame = Instantiate(selectedFrame, existingFrame.position, existingFrame.rotation, existingFrame.parent);
        GameObject newPicture = Instantiate(selectedPicture, existingPicture.position, existingPicture.rotation, newFrame.transform);

        newFrame.transform.localScale = existingFrame.transform.localScale;
        newPicture.transform.localScale = existingPicture.transform.localScale;

        Destroy(existingFrame.gameObject);
        Destroy(existingPicture.gameObject);
    }
}
