using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class VehicleSlot : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI vehicleNameText;
	[SerializeField] private TextMeshProUGUI vehicleTypeText;
	[SerializeField] private Image vehicleImage;
	[SerializeField] private GameObject statsGroupGameObject;
	[SerializeField] private Image firepowerImage;
	[SerializeField] private Image mobilityImage;
	[SerializeField] private Image defenceImage;
	[SerializeField] private Button vehicleSelectButton;

	public int dataIndex = 0;


	public void LoadVehicleData(int index)
	{
		// The default "Select a Vehicle" slot representation
		if (index == -1)
		{
			Debug.Log($"Vehicle Slot :: Loaded Default Empty Slot");
			ResetSlot();
		}
		else
		{
			VehicleLobbyData vData = VehicleData.GetItem(index);

			//Debug.Log($"Loading {vData.name}. Vehicle Select button is null? {vehicleSelectButton == null}");
			// Will be null if this script is within a Player Slot
			if (vehicleSelectButton != null)
			{
				vehicleSelectButton.gameObject.SetActive(true);
				vehicleSelectButton.onClick.AddListener(OnVehicleClicked);
				//Debug.Log($"Assigned Listener");
			}

			this.vehicleNameText.text = vData.name;
			this.vehicleTypeText.text = vData.type.ToString();
			this.vehicleImage.gameObject.SetActive(true);
			this.vehicleImage.sprite = vData.icon;
			this.firepowerImage.transform.parent.gameObject.SetActive(true);
			this.firepowerImage.fillAmount = vData.firepower;
			this.mobilityImage.fillAmount = vData.mobility;
			this.defenceImage.fillAmount = vData.defence;

			statsGroupGameObject.SetActive(true);
			Debug.Log($"Vehicle Slot :: Loaded Vehicle {this.vehicleNameText.text} into slot");
		}
	}

	public void ResetSlot()
	{
		this.vehicleNameText.text = string.Empty;
		this.vehicleTypeText.text = string.Empty;
		this.firepowerImage.transform.parent.gameObject.SetActive(false);
		this.vehicleImage.gameObject.SetActive(false);
		statsGroupGameObject.SetActive(false);
	}

	public async void OnVehicleClicked()
	{
		Debug.Log($"Vehicle Slot :: Clicked on Vehicle for Selection");
		await LobbyManager.Instance.SwapLobbyVehicle(dataIndex);
		UIManager.LobbyUI.chooseVehicleViewGameObject.SetActive(false);
	}

	public void SetStatsGroupVisible(bool choice) => statsGroupGameObject.SetActive(choice);
}