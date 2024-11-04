using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NetEnvetType
{
    Connect = 0,
    Disconnect,
    SendError,
    ReceiveError,
}

public enum NetVEventResult
{
    Fail = -1,
    Sussess = 0,
}

public class NetEventState
{
    public NetEnvetType type;
    public NetVEventResult result;
}