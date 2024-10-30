using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MessageType
{
    Chat = 0,
    CharacterSpawn = 1,
    CharacterUpdate = 2,
    Animation = 3
}

[System.Serializable]
public class NetworkMessage
{
    public MessageType type;
    public string data;

    public NetworkMessage(MessageType type, string data)
    {
        this.type = type;
        this.data = data;
    }
}
