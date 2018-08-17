using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour {

    private MovementManager movementmanager;
    
    private bool hasDoubleJumped = false;

	// Use this for initialization
	void Start () {
        movementmanager = GetComponent<MovementManager>();
    }
	
	// Update is called once per frame
	void Update () {

        bool Left = Input.GetKey(KeyCode.A),
             Right = Input.GetKey(KeyCode.D);

        movementmanager.SetDirection(Left ? -1 : (Right ? 1 : 0), 0);

        if(Input.GetKeyDown(KeyCode.Space) && 
            (movementmanager._grounded || (!movementmanager._grounded && !hasDoubleJumped)))
        {
            movementmanager.Jump();

            if (!movementmanager._grounded && !hasDoubleJumped)
                hasDoubleJumped = true;
            else
                hasDoubleJumped = false;
        }

        if (Input.GetKeyUp(KeyCode.Space))
            movementmanager.CutJump(2);

        if (Input.GetKeyDown(KeyCode.Z))
            movementmanager.Stop();
        if (Input.GetKeyUp(KeyCode.Z))
            movementmanager.Resume();

        if (!movementmanager._grounded && (movementmanager.collisionInfo.collidedLeft && Input.GetKey(KeyCode.A) 
                                          || movementmanager.collisionInfo.collidedRight && Input.GetKey(KeyCode.D)) 
                                          && Input.GetKeyDown(KeyCode.Space))
        {
            movementmanager.WallJump();
        }

        if(Input.GetKeyDown(KeyCode.S))
        {
            movementmanager.FallThrough();
        }
        
	}
}
