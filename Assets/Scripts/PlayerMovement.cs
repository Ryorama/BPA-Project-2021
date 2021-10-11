using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
	public class PlayerMovement : MonoBehaviour
	{

		public CharacterController2D controller;

		public static bool lockMovement = false;

		public static float runSpeed = 30f;

		float horizontalMove = 0f;
		bool jump = false;
		bool crouch = false;

		bool isMobile = false;

		// Update is called once per frame
		void Update()
		{

			if (lockMovement == false)
			{

				horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;

				if (Input.GetButtonDown("Jump"))
				{
					jump = true;
				}

				if (Input.GetButtonDown("Crouch"))
				{
					crouch = true;
				}
				else if (Input.GetButtonUp("Crouch"))
				{
					crouch = false;
				}
			}
		}

		void FixedUpdate()
		{
			// Move our character
			controller.Move(horizontalMove * Time.fixedDeltaTime, crouch, jump);
			jump = false;
		}

		public void Jump()
		{
			jump = true;

		}
	}
}
