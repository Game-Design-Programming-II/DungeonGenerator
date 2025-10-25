using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DestroyMe : MonoBehaviour
{
    private GameObject self;

    private void Awake()
    {
        self = this.gameObject;
    }

    private void Update()
    {
        if (self.GetComponent<TMP_Text>().color.a == 0)
        {
            Destroy(self);
        }
    }
}
