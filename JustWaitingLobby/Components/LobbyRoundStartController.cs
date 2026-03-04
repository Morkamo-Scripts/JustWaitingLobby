using System;
using System.Collections;
using System.Linq;
using Exiled.API.Features;
using LabApi.Events.Arguments.ServerEvents;
using MEC;
using PlayerRoles;
using ProjectMER.Features;
using UnityEngine;

namespace JustWaitingLobby.Components;

public class LobbyRoundStartController
{
    private const float FixedRoundStartBufferSeconds = 0.35f;

    private Coroutine _roundStartBufferCoroutine;
    private bool _skipRoundStartInterception;
    private bool _isRoundStartBufferActive;

    public void HandleRoundStarting(RoundStartingEventArgs ev, bool isWaitingForPlayers, Action onRoundStartBufferBegin, Action<Player> movePlayerToSpectator)
    {
        if (!ev.IsAllowed)
            return;

        if (_skipRoundStartInterception)
        {
            _skipRoundStartInterception = false;
            return;
        }

        if (!isWaitingForPlayers || _isRoundStartBufferActive)
        {
            ev.IsAllowed = false;
            return;
        }

        ev.IsAllowed = false;
        _isRoundStartBufferActive = true;
        onRoundStartBufferBegin?.Invoke();

        StopRoundStartBuffer();
        _roundStartBufferCoroutine = CoroutineRunner.Run(RoundStartBufferCoroutine(movePlayerToSpectator));
    }

    public void Reset()
    {
        StopRoundStartBuffer();
        _skipRoundStartInterception = false;
        _isRoundStartBufferActive = false;
    }

    private void StopRoundStartBuffer()
    {
        if (_roundStartBufferCoroutine == null)
            return;

        CoroutineRunner.Stop(_roundStartBufferCoroutine);
        _roundStartBufferCoroutine = null;
    }

    private IEnumerator RoundStartBufferCoroutine(Action<Player> movePlayerToSpectator)
    {
        if (MapUtils.TryGetMapData(Plugin.Instance.LobbyHandler.LobbyMapName, out var map))
        {
            MapUtils.UnloadMap(map.Name);
            Log.Info($"Lobby map '{map.Name}' has been unloaded!");
        }
        
        yield return new WaitForSeconds(FixedRoundStartBufferSeconds);

        foreach (Player player in Player.List.ToList())
        {
            RoleTypeId role = player.Role.Type;
            if (role == RoleTypeId.Overwatch || role == RoleTypeId.Filmmaker)
                continue;

            movePlayerToSpectator?.Invoke(player);
        }

        yield return new WaitForSeconds(0.3f);

        Round.IsLobbyLocked = false;
        _skipRoundStartInterception = true;
        _isRoundStartBufferActive = false;
        _roundStartBufferCoroutine = null;
        Round.Start();
    }
}

