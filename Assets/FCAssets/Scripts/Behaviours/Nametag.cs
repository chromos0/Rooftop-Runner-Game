using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nametag : MonoBehaviour
{
    private Transform Player;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {   
        if (GameObject.Find("Player") != null){
            Transform player = GameObject.Find("Player").transform;

            Vector3 directionToPlayer = player.position - transform.position;

            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);

            lookRotation *= Quaternion.Euler(0, 180f, 0);

            transform.rotation = lookRotation;
        }
    }
}
