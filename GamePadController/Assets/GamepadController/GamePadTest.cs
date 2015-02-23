using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class GamePadTest : MonoBehaviour
{

    GamePadController.Controller gamePad;

    float speed = 45f;
    Vector3 axis;

    void Start()
    {
        gamePad = GamePadController.GamePadOne;
    }

	void Update () {

        if (gamePad.A.Held)
            gamePad.SetVibration(100);
        else
            gamePad.StopVibration();

        axis = new Vector3(gamePad.LeftStick.X, gamePad.LeftStick.Y, 0);
		transform.Rotate (axis * Time.deltaTime * speed);
        
        


    }

}


