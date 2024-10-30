using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    public Network NetworkComponent {  get; private set; }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            NetworkComponent = GetComponent<Network>();
            if(NetworkComponent == null)
            {
                NetworkComponent = gameObject.AddComponent<Network>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Network GetNetwork()
    {
        return NetworkComponent;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
