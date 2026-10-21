using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using PlayerRoles;
using UnityEngine;

namespace JustWaitingLobby.Components;

public class LobbyPlayerSettings
{
    public bool EnableLobbyGodMode { get; set; }
    public bool EnableLobbyTeleport { get; set; }
    public bool UseRoomTeleport { get; set; }
    public LobbyPosition StaticLobbyPosition { get; set; }
    public LobbyPosition RoomTeleportOffset { get; set; }
    public HashSet<RoomType> LobbyRooms { get; set; }
}

public class LobbyPlayerController
{
    private readonly HashSet<int> _godModePlayers = new();
    private readonly HashSet<int> _tutorialSwitchInProgress = new();
    private Coroutine _lobbyStateCoroutine;

    public void ApplyLobbyState(Player player, LobbyPlayerSettings settings, Action<Player, RoleTypeId> visibilityUpdate)
    {
        if (player is null)
            return;

        RoleTypeId role = player.Role.Type;
        if (role == RoleTypeId.None || role == RoleTypeId.Spectator || role == RoleTypeId.Overwatch)
        {
            SetPlayerGodMode(player, false);
            visibilityUpdate?.Invoke(player, role);
            return;
        }

        player.IsUsingStamina = false;

        SetPlayerGodMode(player, settings.EnableLobbyGodMode && role == RoleTypeId.Tutorial);

        if (settings.EnableLobbyTeleport && role == RoleTypeId.Tutorial)
            TeleportLobbyPlayer(player, settings);

        visibilityUpdate?.Invoke(player, role);
    }

    public bool TryForceTutorialRole(Player player)
    {
        if (player is null)
            return false;

        RoleTypeId role = player.Role.Type;
        if (role == RoleTypeId.Tutorial)
        {
            _tutorialSwitchInProgress.Remove(player.Id);
            return false;
        }

        if (role == RoleTypeId.Spectator || role == RoleTypeId.Overwatch)
            return false;

        if (!_tutorialSwitchInProgress.Add(player.Id))
            return true;

        try
        {
            player.Role.Set(RoleTypeId.Tutorial, SpawnReason.ForceClass, RoleSpawnFlags.None);
            return true;
        }
        catch (Exception e)
        {
            _tutorialSwitchInProgress.Remove(player.Id);
            Log.Error($"[JustWaitingLobby]: {e}");
            return false;
        }
    }

    public void StartLobbyStateLoop(Func<bool> isWaitingProvider, Func<LobbyPlayerSettings> settingsProvider)
    {
        StopLobbyStateLoop();
        _lobbyStateCoroutine = CoroutineRunner.Run(LobbyStateCoroutine(isWaitingProvider, settingsProvider));
    }

    public void StopLobbyStateLoop()
    {
        if (_lobbyStateCoroutine == null)
            return;

        CoroutineRunner.Stop(_lobbyStateCoroutine);
        _lobbyStateCoroutine = null;
    }

    public void DisableTrackedGodMode()
    {
        if (_godModePlayers.Count == 0)
            return;

        foreach (int playerId in _godModePlayers.ToList())
        {
            Player player = Player.Get(playerId);
            if (player is null)
                continue;

            player.IsGodModeEnabled = false;
        }
    }

    public void SetPlayerGodMode(Player player, bool state)
    {
        if (player is null)
            return;

        player.IsGodModeEnabled = state;

        if (state)
            _godModePlayers.Add(player.Id);
        else
            _godModePlayers.Remove(player.Id);
    }

    public void ClearTutorialSwitchTracking()
    {
        _tutorialSwitchInProgress.Clear();
    }

    public void ClearTracking()
    {
        _godModePlayers.Clear();
        _tutorialSwitchInProgress.Clear();
    }

    public void OnPlayerLeft(int playerId)
    {
        _godModePlayers.Remove(playerId);
        _tutorialSwitchInProgress.Remove(playerId);
    }

    private IEnumerator LobbyStateCoroutine(Func<bool> isWaitingProvider, Func<LobbyPlayerSettings> settingsProvider)
    {
        while (isWaitingProvider())
        {
            LobbyPlayerSettings settings = settingsProvider();
            foreach (Player player in Player.List)
                RefreshLobbyBuffs(player, settings.EnableLobbyGodMode);

            yield return new WaitForSeconds(0.2f);
        }
        // ReSharper disable once IteratorNeverReturns
    }

    private void RefreshLobbyBuffs(Player player, bool enableLobbyGodMode)
    {
        if (player is null)
            return;

        SetPlayerGodMode(player, enableLobbyGodMode && player.Role.Type == RoleTypeId.Tutorial);
    }

    private void TeleportLobbyPlayer(Player player, LobbyPlayerSettings settings)
    {
        try
        {
            if (settings.UseRoomTeleport && TryGetRandomLobbyRoom(settings, out Room room))
            {
                player.Teleport(room.Position + settings.RoomTeleportOffset.ToVector3());
                return;
            }

            player.Teleport(settings.StaticLobbyPosition.ToVector3());
        }
        catch (Exception e)
        {
            Log.Error($"[JustWaitingLobby]: {e}");
        }
    }

    private bool TryGetRandomLobbyRoom(LobbyPlayerSettings settings, out Room room)
    {
        room = null;

        if (settings.LobbyRooms == null || settings.LobbyRooms.Count == 0)
            return false;

        List<Room> candidates = Room.List.Where(r => settings.LobbyRooms.Contains(r.Type)).ToList();
        if (candidates.Count == 0)
            return false;

        room = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return true;
    }
}

