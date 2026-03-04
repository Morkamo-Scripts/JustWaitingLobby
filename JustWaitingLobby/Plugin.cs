using System;
using Exiled.API.Features;
using HarmonyLib;
using JustWaitingLobby.Handlers;
using labServerEvents = LabApi.Events.Handlers.ServerEvents;
using events = Exiled.Events.Handlers;

namespace JustWaitingLobby
{
    public class Plugin : Plugin<Config>
    {
        public override string Name => nameof(JustWaitingLobby);
        public override string Prefix => Name;
        public override string Author => "Morkamo";
        public override Version RequiredExiledVersion => new(9, 13, 0);
        public override Version Version => new(1, 0, 0);
        
        public static Plugin Instance;
        public Harmony Harmony;
        public LobbyHandler LobbyHandler;
        
        public override void OnEnabled()
        {
            Instance = this;
            Harmony = new Harmony("ru.morkamo.justwaitinglobby");
            Harmony.PatchAll();
            LobbyHandler = Config.LobbyHandler;
            RegisterEvents();

            if (Round.IsLobby)
                LobbyHandler.OnWaitingForPlayers();

            base.OnEnabled();
        }

        public override void OnDisabled() 
        {
            LobbyHandler?.OnDisabled();
            UnregisterEvents();
            LobbyHandler = null;
            Harmony.UnpatchAll();
            Harmony = null;
            Instance = null;
            base.OnDisabled();
        }
        
        private void RegisterEvents()
        {
            events.Server.WaitingForPlayers += LobbyHandler.OnWaitingForPlayers;
            events.Server.RoundStarted += LobbyHandler.OnRoundStarted;
            labServerEvents.RoundStarting += LobbyHandler.OnRoundStarting;
            events.Player.Verified += LobbyHandler.OnVerified;
            events.Player.Spawned += LobbyHandler.OnSpawned;
            events.Player.ChangingRole += LobbyHandler.OnChangingRole;
            events.Player.Left += LobbyHandler.OnLeft;
            events.Player.InteractingDoor += LobbyHandler.OnInteractingDoor;
            events.Player.InteractingElevator += LobbyHandler.OnInteractingElevator;
            events.Player.DamagingWindow += LobbyHandler.OnDamagingWindow;
            events.Player.DamagingDoor += LobbyHandler.OnDamagingDoor;
        }

        private void UnregisterEvents()
        {
            events.Server.WaitingForPlayers -= LobbyHandler.OnWaitingForPlayers;
            events.Server.RoundStarted -= LobbyHandler.OnRoundStarted;
            labServerEvents.RoundStarting -= LobbyHandler.OnRoundStarting;
            events.Player.Verified -= LobbyHandler.OnVerified;
            events.Player.Spawned -= LobbyHandler.OnSpawned;
            events.Player.ChangingRole -= LobbyHandler.OnChangingRole;
            events.Player.Left -= LobbyHandler.OnLeft;
            events.Player.InteractingDoor -= LobbyHandler.OnInteractingDoor;
            events.Player.InteractingElevator -= LobbyHandler.OnInteractingElevator;
            events.Player.DamagingWindow -= LobbyHandler.OnDamagingWindow;
            events.Player.DamagingDoor -= LobbyHandler.OnDamagingDoor;
        }
    }
}

