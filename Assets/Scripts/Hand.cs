using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    private float animTime = 0f;
    private Vector3 origin, destination;
    [SerializeField] private Transform target;

    private void Start()
    {
        origin = transform.position;
        destination = transform.position;
        destination.x = target.position.x;
    }

    private void Update()
    {
        if(animTime < 1f)
        {
            animTime += Time.deltaTime;

            transform.position = Vector3.Lerp(origin, destination, animTime);
        }
    }
}
