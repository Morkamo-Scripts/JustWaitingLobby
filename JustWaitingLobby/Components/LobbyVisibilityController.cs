using System;
using System.Collections.Generic;
using System.Reflection;
using Exiled.API.Features;
using GameCore;
using HarmonyLib;
using Mirror;
using PlayerRoles;

namespace JustWaitingLobby.Components;

public class LobbyVisibilityController
{
    private static readonly MethodInfo AddObserverMethod = AccessTools.Method(typeof(NetworkIdentity), "AddObserver");
    private static readonly MethodInfo RemoveObserverMethod = AccessTools.Method(typeof(NetworkIdentity), "RemoveObserver");
    private static readonly MethodInfo RemoveFromObservingMethod = AccessTools.Method(typeof(NetworkConnectionToClient), "RemoveFromObserving");

    private readonly HashSet<int> _hiddenPlayers = new();

    public void UpdateRoundStartVisibility(Player player, RoleTypeId role, bool hideLobbyBanner, bool isWaitingForPlayers)
    {
        if (!hideLobbyBanner || !isWaitingForPlayers)
            return;

        if (role == RoleTypeId.None || role == RoleTypeId.Spectator || role == RoleTypeId.Overwatch)
        {
            TryShowRoundStartForPlayer(player);
            _hiddenPlayers.Remove(player.Id);
            return;
        }

        if (TryHideRoundStartForPlayer(player))
            _hiddenPlayers.Add(player.Id);
    }

    public void RestoreHiddenPlayers()
    {
        if (_hiddenPlayers.Count == 0)
            return;

        foreach (int playerId in _hiddenPlayers)
        {
            Player player = Player.Get(playerId);
            if (player is null)
                continue;

            TryShowRoundStartForPlayer(player);
        }
    }

    public void RemovePlayer(int playerId)
    {
        _hiddenPlayers.Remove(playerId);
    }

    public void Clear()
    {
        _hiddenPlayers.Clear();
    }

    private static bool TryGetRoundStartIdentity(out NetworkIdentity identity)
    {
        identity = null;

        RoundStart roundStart = RoundStart.singleton;
        if (roundStart == null)
            return false;

        identity = roundStart.netIdentity;
        return identity != null;
    }

    private static bool TryGetConnection(Player player, out NetworkConnectionToClient connection)
    {
        connection = player.ReferenceHub?.connectionToClient;
        return connection != null;
    }

    private static bool TryHideRoundStartForPlayer(Player player)
    {
        try
        {
            if (RemoveFromObservingMethod is null || RemoveObserverMethod is null)
            {
                Log.Error("[JustWaitingLobby] Не найдены внутренние Mirror-методы для скрытия RoundStart.");
                return false;
            }

            if (!TryGetRoundStartIdentity(out NetworkIdentity identity) || !TryGetConnection(player, out NetworkConnectionToClient connection))
                return false;

            if (!connection.observing.Contains(identity))
                return true;

            RemoveFromObservingMethod.Invoke(connection, new object[] { identity, false });
            RemoveObserverMethod.Invoke(identity, new object[] { connection });
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"[JustWaitingLobby]: {e}");
            return false;
        }
    }

    private static void TryShowRoundStartForPlayer(Player player)
    {
        try
        {
            if (AddObserverMethod is null)
                return;

            if (!TryGetRoundStartIdentity(out NetworkIdentity identity) || !TryGetConnection(player, out NetworkConnectionToClient connection))
                return;

            if (connection.observing.Contains(identity))
                return;

            AddObserverMethod.Invoke(identity, new object[] { connection });
        }
        catch (Exception e)
        {
            Log.Error($"[JustWaitingLobby]: {e}");
        }
    }
}

