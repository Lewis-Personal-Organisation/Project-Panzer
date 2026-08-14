using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    // Used to set client authoritative so clients can move the Network Transform.
    // This imposes state to the server and puts trust on clients
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
