using System;
using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using GameCore;
using JustWaitingLobby.Components;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using MEC;
using PlayerRoles;
using ProjectMER.Features;
using ProjectMER.Features.Serializable;
using UnityEngine;

namespace JustWaitingLobby.Handlers;

public class LobbyHandler
{
    private readonly LobbyVisibilityController _visibilityController = new();
    private readonly LobbyPlayerController _playerController = new();
    private readonly LobbyTimerController _timerController = new();
    private readonly LobbyRoundStartController _roundStartController = new();

    private bool _isWaitingForPlayers;
    private bool _isMapLoaded = false;

    [Description("Скрывать стандартный экран ожидания у игроков")]
    public bool HideLobbyBanner { get; set; } = true;

    [Description("Показывать таймер ожидания через RueI")]
    public bool ShowLobbyTimer { get; set; } = true;

    [Description("Вертикальная позиция таймера RueI (Y)")]
    public ushort TimerVerticalPosition { get; set; } = 850;

    [Description("Текст таймера. Плейсхолдер: {seconds}")]
    public string TimerMessage { get; set; } = "<b><size=70>Игра начнётся через: {seconds} сек.</b></size>";

    [Description("Текст, когда запуск раунда приостановлен")]
    public string WaitingPlayersMessage { get; set; } = "<size=70><b>Ожидание игроков...</b></size>";

    [Description("Дополнительное сообщение в лобби")]
    public string AdditionalInfoMessage { get; set; } = "<b><size=40>Для перехода в наблюдатели нажмите кнопку использования Noclip.</b></size>";

    [Description("Вертикальная позиция дополнительного сообщения RueI (Y)")]
    public ushort AdditionalInfoVerticalPosition { get; set; } = 800;

    [Description("Автоматически ставить игрока в Tutorial в лобби")]
    public bool AutoSpawnTutorialInLobby { get; set; } = true;

    [Description("Выдавать GodMode игрокам Tutorial в лобби")]
    public bool EnableLobbyGodMode { get; set; } = true;

    [Description("Телепортировать Tutorial в зону ожидания")]
    public bool EnableLobbyTeleport { get; set; } = true;

    [Description("Использовать случайную комнату вместо статичных координат")]
    public bool UseRoomTeleport { get; set; } = false;

    [Description("Карта ProjectMER, загружаемая при при инициализации лобби")]
    public string LobbyMapName { get; set; } = "ParkourLobbyMap";
    
    [Description("Координаты телепорта при статичном режиме")]
    public LobbyPosition StaticLobbyPosition { get; set; } = new();

    [Description("Смещение от центра выбранной комнаты")]
    public LobbyPosition RoomTeleportOffset { get; set; } = new() { Y = 1.2f };

    [Description("Список комнат для случайного телепорта")]
    public HashSet<RoomType> LobbyRooms { get; set; } = new()
    {
        RoomType.EzConference,
        RoomType.EzCafeteria,
        RoomType.EzShelter
    };

    /* 
    public void TogglingNoclipSwitcher(TogglingNoClipEventArgs ev)
    {
        if (!_isWaitingForPlayers)
        {
            if (!Round.IsStarted)
                ev.IsAllowed = false;
            return;
        }

        ev.IsAllowed = false;
        RoleTypeId role = ev.Player.Role.Type == RoleTypeId.Tutorial
            ? RoleTypeId.None
            : RoleTypeId.Tutorial;
        ev.Player.Role.Set(role, SpawnReason.ForceClass);
        _playerController.ApplyLobbyState(ev.Player, BuildPlayerSettings(), UpdateRoundStartVisibility);
        _timerController.SetVisible(ev.Player, ShowLobbyTimer && ev.Player.IsAlive);
    }
    */

    public void OnWaitingForPlayers()
    {
        _isWaitingForPlayers = true;
        _visibilityController.Clear();
        _playerController.ClearTracking();
        _roundStartController.Reset();

        _timerController.Start(ShowLobbyTimer, () => _isWaitingForPlayers, () => TimerVerticalPosition, BuildTimerText,
            () => AdditionalInfoVerticalPosition, () => AdditionalInfoMessage);
        _playerController.StartLobbyStateLoop(() => _isWaitingForPlayers, BuildPlayerSettings);

        LobbyPlayerSettings settings = BuildPlayerSettings();
        foreach (Player player in Player.List)
            _playerController.ApplyLobbyState(player, settings, UpdateRoundStartVisibility);

        if (_isMapLoaded)
            _isMapLoaded = false;
    }

    public void OnRestartingRound()
    {
        _isWaitingForPlayers = false;
        _isMapLoaded = false;

        _timerController.Stop(clearDisplay: false);
        _playerController.StopLobbyStateLoop();
        _roundStartController.Reset();

        _visibilityController.Clear();
        _playerController.ClearTracking();
    }

    public void OnRoundStarted()
    {
        _isWaitingForPlayers = false;
        _timerController.Stop(clearDisplay: true);
        _playerController.StopLobbyStateLoop();
        _roundStartController.Reset();
        _visibilityController.RestoreHiddenPlayers();
        _playerController.DisableTrackedGodMode();
        _visibilityController.Clear();
        _playerController.ClearTracking();
    }

    public void OnDisabled()
    {
        _isWaitingForPlayers = false;
        _timerController.Stop(clearDisplay: true);
        _playerController.StopLobbyStateLoop();
        _roundStartController.Reset();
        _visibilityController.RestoreHiddenPlayers();
        _playerController.DisableTrackedGodMode();
        _visibilityController.Clear();
        _playerController.ClearTracking();
    }

    public void OnLeft(LeftEventArgs ev)
    {
        _visibilityController.RemovePlayer(ev.Player.Id);
        _playerController.OnPlayerLeft(ev.Player.Id);
    }

    public void OnInteractingDoor(InteractingDoorEventArgs ev)
    {
        if (Round.IsLobby)
        {
            ev.IsAllowed = false;
        }
    }
    
    public void OnInteractingElevator(InteractingElevatorEventArgs ev)
    {
        if (Round.IsLobby)
            ev.IsAllowed = false;
    }
    
    public void OnDamagingWindow(DamagingWindowEventArgs ev)
    {
        if (Round.IsLobby)
            ev.IsAllowed = false;
    }
    
    public void OnDamagingDoor(DamagingDoorEventArgs ev)
    {
        if (Round.IsLobby)
            ev.IsAllowed = false;
    }

    public void OnSpawned(SpawnedEventArgs ev)
    {
        if (!_isWaitingForPlayers)
            return;

        _timerController.SetVisible(ev.Player, ShowLobbyTimer && ev.Player.IsAlive);

        if (ev.Player.Role.Type == RoleTypeId.None)
        {
            _playerController.ApplyLobbyState(ev.Player, BuildPlayerSettings(), UpdateRoundStartVisibility);
            return;
        }

        if (AutoSpawnTutorialInLobby && _playerController.TryForceTutorialRole(ev.Player))
            return;
        
        ev.Player.Rotation = new Quaternion(0, 0.7f, 0, 0.7f);
        _playerController.ApplyLobbyState(ev.Player, BuildPlayerSettings(), UpdateRoundStartVisibility);
    }

    public void OnVerified(VerifiedEventArgs ev)
    {
        if (MapUtils.TryGetMapData(LobbyMapName, out var map) && !_isMapLoaded)
        {
            _isMapLoaded = true;
            MapUtils.LoadMap(map.Name);
            Log.Info($"Lobby map '{map.Name}' has been loaded!");
        }
        
        if (!_isWaitingForPlayers)
            return;

        if (AutoSpawnTutorialInLobby && _playerController.TryForceTutorialRole(ev.Player))
            return;
    }

    public void OnChangingRole(ChangingRoleEventArgs ev)
    {
        if (!_isWaitingForPlayers)
            return;

        UpdateRoundStartVisibility(ev.Player, ev.NewRole);
    }

    public void OnRoundStarting(RoundStartingEventArgs ev)
    {
        _roundStartController.HandleRoundStarting(
            ev,
            _isWaitingForPlayers,
            OnRoundStartBufferBegin,
            MovePlayerToRoundStartWaiting);
    }

    private string BuildTimerText()
    {
        int seconds = Math.Max(0, Convert.ToInt32(Round.LobbyWaitingTime));
        string timer = Round.IsLobbyLocked || Round.LobbyWaitingTime <= 0
            ? WaitingPlayersMessage
            : TimerMessage.Replace("{seconds}", seconds.ToString());
        return timer;
    }

    private LobbyPlayerSettings BuildPlayerSettings()
    {
        return new LobbyPlayerSettings
        {
            EnableLobbyGodMode = EnableLobbyGodMode,
            EnableLobbyTeleport = EnableLobbyTeleport,
            UseRoomTeleport = UseRoomTeleport,
            StaticLobbyPosition = StaticLobbyPosition,
            RoomTeleportOffset = RoomTeleportOffset,
            LobbyRooms = LobbyRooms
        };
    }

    private void UpdateRoundStartVisibility(Player player, RoleTypeId role)
    {
        _visibilityController.UpdateRoundStartVisibility(player, role, HideLobbyBanner, _isWaitingForPlayers);
    }

    private void OnRoundStartBufferBegin()
    {
        _isWaitingForPlayers = false;
        Round.IsLobbyLocked = true;
        _timerController.Stop(clearDisplay: true);
        _playerController.StopLobbyStateLoop();
        _playerController.ClearTutorialSwitchTracking();
    }

    private void MovePlayerToRoundStartWaiting(Player player)
    {
        try
        {
            _playerController.SetPlayerGodMode(player, false);
            player.Role.Set(RoleTypeId.None, SpawnReason.ForceClass, RoleSpawnFlags.None);
            _visibilityController.UpdateRoundStartVisibility(player, RoleTypeId.None, HideLobbyBanner, true);
        }
        catch (Exception e)
        {
            Log.Error($"[JustWaitingLobby]: {e}");
        }
    }
}



