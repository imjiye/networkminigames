using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HostGuestManager : MonoBehaviour
{
    public TMP_InputField idInput;
    public GameObject hostguestPanel;

    private Network network;

    // Start is called before the first frame update
    void Start()
    {
        GameObject networkManagerPrefab = Resources.Load<GameObject>("NetworkManager");
        if (networkManagerPrefab != null)
        {
            Instantiate(networkManagerPrefab);
        }
        else
        {
            GameObject networkManagerobj = new GameObject("NetworkManager");
            networkManagerobj.AddComponent<NetworkManager>();
        }

        network = NetworkManager.Instance.GetNetwork();
        hostguestPanel.SetActive(true);
    }

    public void SelectHost()
    {
        if (string.IsNullOrEmpty(idInput.text))
        {
            Debug.LogError("Please enter a player name!");
            return;
        }

        Debug.Log("SelectHostBtn");
        network.PlayerName = idInput.text;
        network.HostStart(10000, 10);
        CharDataManager.instance.Role = UserRole.Host;
        CharDataManager.instance.PlayerName = idInput.text;
        hostguestPanel.SetActive(false);
    }

    public void SelectGuest()
    {
        if (string.IsNullOrEmpty(idInput.text))
        {
            Debug.LogError("Please enter a player name!");
            return;
        }

        Debug.Log("SelectGuestBtn");
        network.PlayerName = idInput.text;
        network.GuestStart("127.0.0.1", 10000);
        CharDataManager.instance.Role = UserRole.Guest;
        CharDataManager.instance.PlayerName = idInput.text;
        hostguestPanel.SetActive(false);
    }

}
