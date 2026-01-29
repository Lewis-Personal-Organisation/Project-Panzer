using Sirenix.OdinInspector;
using UnityEngine;

public class Rotation : MonoBehaviour
{
	public enum RotationType
	{
		GradualVector,
		LookAt,
		LookAtLineOfSiteX,
		LookAtLineOfSiteY,
		LookAtLineOfSiteZ,
		LookAway,
		Copy,
		CopyX,
		CopyY,
		CopyZ,
		LerpTowards,
		SlerpTowards,
		GradualTowards,
		RotateAround,
	}

	[SerializeField] private RotationType rotationType;

	[SerializeField] private Transform Object;
	private Quaternion startRotation;
	[SerializeField] private Transform Target;
	[SerializeField] private float speed;
	[SerializeField] private float Multiplier;

	[ShowIf("rotationType",  RotationType.GradualVector)]
	[SerializeField] private Vector3 gradualVector;

	private float timer;
	private Quaternion cachedRot;


	private void Start()
	{
		startRotation = Object.rotation;
	}

	/// <summary>
	/// Apply the type of rotation
	/// </summary>
	private void FixedUpdate()
	{
		switch (rotationType)
		{
			// Rotates Player gradually on each angle
			case RotationType.GradualVector:
				Object.Rotate(gradualVector * Time.deltaTime);
				Debug.DrawLine(Object.position, Object.position + Object.forward * 10F, Color.red);
				break;

			// Causes Player to look directly at Point on the forward Axis
			case RotationType.LookAt:
				Object.LookAt(Target);
				Debug.DrawLine(Object.position, Target.position, Color.red);
				break;

			// Causes the Player to look away from the Points position
			case RotationType.LookAway:
				Object.rotation = Quaternion.LookRotation(Object.transform.position - Target.transform.position);
				Debug.DrawLine(Object.position, Object.position + Object.transform.forward * Multiplier, Color.red, Time.deltaTime);
				break;

			// Causes Player to look at Points Gaze on X axis, multiplied
			case RotationType.LookAtLineOfSiteX:
				Debug.DrawLine(Object.position, Target.position + (Target.right * Multiplier), Color.magenta, Time.deltaTime);
				Debug.DrawLine(Target.position, Target.position + (Target.right * Multiplier), Color.magenta, Time.deltaTime);
				Object.LookAt(Target.position + (Target.right * Multiplier));
				break;

			// Causes Player to look at Points Gaze on Y axis, multiplied
			case RotationType.LookAtLineOfSiteY:
				Debug.DrawLine(Object.position, Target.position + (Target.up * Multiplier), Color.magenta, Time.deltaTime);
				Debug.DrawLine(Target.position, Target.position + (Target.up * Multiplier), Color.magenta, Time.deltaTime);
				Object.LookAt(Target.position + (Target.up * Multiplier));
				break;

			// Causes Player to look at Points Gaze on Z axis, multiplied
			case RotationType.LookAtLineOfSiteZ:
				Debug.DrawLine(Object.position, Target.position + (Target.forward * Multiplier), Color.magenta, Time.deltaTime);
				Debug.DrawLine(Target.position, Target.position + (Target.forward * Multiplier), Color.magenta, Time.deltaTime);
				Object.LookAt(Target.position + (Target.forward * Multiplier));
				break;

			// Takes Point's rotation and applies it to Player
			case RotationType.Copy:
				Object.rotation = Target.rotation;
				Debug.DrawLine(Object.position, Object.position + (Object.forward * 10F), Color.magenta, Time.deltaTime);
				Debug.DrawLine(Target.position, Target.position + (Target.forward * 10F), Color.magenta, Time.deltaTime);
				break;

			// Copies the X axis rotation value of Point, maintaining the other axis values
			case RotationType.CopyX:
				Object.rotation = Quaternion.Euler(Target.rotation.eulerAngles.x, Object.rotation.eulerAngles.y, Object.rotation.eulerAngles.z);
				break;

			// Copies the Y axis rotation value of Point, maintaining the other axis values
			case RotationType.CopyY:
				Object.rotation = Quaternion.Euler(Object.rotation.eulerAngles.x, Target.rotation.eulerAngles.y, Object.rotation.eulerAngles.z);
				break;

			// Copies the Z axis rotation value of Point, maintaining the other axis values
			case RotationType.CopyZ:
				Object.rotation = Quaternion.Euler(Object.rotation.eulerAngles.x, Object.rotation.eulerAngles.y, Target.rotation.eulerAngles.z);
				break;

			// Linearly interpolate from Start to End using speed
			// Advantages: A percentage of travel between Start and End can be measured
			// Limitations: Not advised to move either the Start or End positions while running
			// Use Case: Grenade, UI
			case RotationType.LerpTowards:
				if (cachedRot != Target.rotation)
				{
					timer = 0;
					startRotation = Object.rotation;
				}
				else
					timer += Time.deltaTime;

				Object.rotation = Quaternion.Lerp(startRotation, Target.rotation, timer * speed);

				cachedRot = Target.rotation;
				break;


			// Spherically interpolate from Start to End using speed
			// Advantages: A percentage of travel between Start and End can be measured
			// Limitations: Not advised to move either the Start or End positions while running
			// Use Case: Grenade, UI
			case RotationType.SlerpTowards:
				if (cachedRot != Target.rotation)
				{
					timer = 0;
					startRotation = Object.rotation;
				}
				else
					timer += Time.deltaTime;

				Object.rotation = Quaternion.Slerp(startRotation, Target.rotation, timer * speed);

				cachedRot = Target.rotation;
				break;

			// Move Towards Target using a specific speed
			// Advantages: Start and End can be moved, travel over time will stay constant
			// Limitations: A percentage of travel can't be measured between Start and End
			// Use Case: Missile, Constant Follow
			case RotationType.GradualTowards:
				Object.rotation = Quaternion.RotateTowards(Object.rotation, Target.rotation, speed * Time.deltaTime);
				break;

			case RotationType.RotateAround:
				Object.RotateAround(Target.position, Vector3.up, Multiplier * Time.deltaTime);
				break;
		}
	}
}