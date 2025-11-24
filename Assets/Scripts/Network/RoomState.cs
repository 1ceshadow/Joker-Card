using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Holds the authoritative list of player netIds in the room as a SyncList.
/// Clients subscribe to changes and update their RoomUI accordingly.
/// Attach this to the same GameObject as `NetworkManagerCustom`.
/// </summary>
public class RoomState : NetworkBehaviour
{
    public readonly SyncList<uint> roomPlayerIds = new SyncList<uint>();

    public override void OnStartClient()
    {
        base.OnStartClient();
        // subscribe to list changes
        roomPlayerIds.Callback += OnRoomPlayersChanged;

        // initial populate (in case list was already populated)
        RefreshRoomUIFromIds();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        roomPlayerIds.Callback -= OnRoomPlayersChanged;
    }

    private void OnRoomPlayersChanged(SyncList<uint>.Operation op, int index, uint oldItem, uint newItem)
    {
        // Whenever the server changes the list, refresh the room UI from locally spawned PlayerData
        RefreshRoomUIFromIds();
    }

    private void RefreshRoomUIFromIds()
    {
        RoomUI ui = FindFirstObjectByType<RoomUI>();
        if (ui == null)
            return;

        List<PlayerData> players = new List<PlayerData>();
        // use client-side spawned dictionary
        foreach (uint netId in roomPlayerIds)
        {
            if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                PlayerData pd = identity.GetComponent<PlayerData>();
                if (pd != null)
                    players.Add(pd);
            }
        }

        ui.UpdatePlayerList(players);
    }
}
