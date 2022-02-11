using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SawMove : MonoBehaviour
{
    [SerializeField] private Transform leftRay;

    [SerializeField] private Transform rightRay;

    private float side = 1;

    private Rigidbody _rigidbody;
    // Start is called before the first frame update
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!Physics.Raycast(leftRay.position, Vector3.down, 10, 1<<6) && side == 1)
        {
            side = -1;
        }
        if (!Physics.Raycast(rightRay.position, Vector3.down, 10, 1<<6) && side == -1)
        {
            side = 1;
        }
        
        _rigidbody.MovePosition(_rigidbody.transform.position + Vector3.right*( 10 * Time.fixedDeltaTime * side));
    }
}
