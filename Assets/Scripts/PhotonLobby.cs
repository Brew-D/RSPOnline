using Photon.Realtime;
using UnityEngine;

public static class PhotonLobbySettings
{
    public static readonly TypedLobby RoomListLobby = new TypedLobby("RoomListLobby", LobbyType.SqlLobby);
}
