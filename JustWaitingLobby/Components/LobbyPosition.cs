using UnityEngine;

namespace JustWaitingLobby.Components;

public class LobbyPosition
{
    public float X { get; set; } = 106.2f;
    public float Y { get; set; } = 296f;
    public float Z { get; set; } = -43.1f;

    public Vector3 ToVector3() => new(X, Y, Z);
}

