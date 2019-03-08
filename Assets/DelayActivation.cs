using System.Collections;
using UnityEngine;

public class DelayActivation : MonoBehaviour
{
    public GameObject[] DelayedObjects;

    // Start is called before the first frame update
    void Start()
    {
    }

    private void OnEnable()
    {
        StartCoroutine(nameof(ShowAppBar));
    }

    private IEnumerator ShowAppBar()
    {
        yield return null;
        foreach (var delayedObject in DelayedObjects)
        {
            delayedObject.SetActive(true);
        }   
    }

}
