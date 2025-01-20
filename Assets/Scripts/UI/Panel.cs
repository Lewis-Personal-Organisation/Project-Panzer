using UnityEngine;

public class Panel : MonoBehaviour
{
	[SerializeField] private GameObject panel;
	public virtual void Toggle(bool activeState) => panel.SetActive(activeState);
	public bool panelIsActive => panel.activeSelf;
}