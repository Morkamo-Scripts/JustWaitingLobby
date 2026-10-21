using System;
using System.Collections;
using RueI.API;
using RueI.API.Elements;
using Exiled.API.Features;
using UnityEngine;

namespace JustWaitingLobby.Components;

public class LobbyTimerController
{
    private readonly Tag _lobbyTimerTag = new("WaitingLobbyTimer");
    private readonly Tag _additionalInfoTag = new("WaitingLobbyAdditionalInfo");
    private Coroutine _timerCoroutine;

    public void SetVisible(Player player, bool visible)
    {
        RueDisplay display = RueDisplay.Get(player);
        display.SetVisible(_lobbyTimerTag, visible);
        display.SetVisible(_additionalInfoTag, visible);
        display.Update();
    }

    public void Start(bool showLobbyTimer, Func<bool> isWaitingProvider, Func<ushort> verticalPositionProvider, Func<string> timerTextProvider,
        Func<ushort> additionalInfoPositionProvider, Func<string> additionalInfoTextProvider)
    {
        Stop(clearDisplay: false);

        if (!showLobbyTimer)
            return;

        _timerCoroutine = CoroutineRunner.Run(TimerCoroutine(isWaitingProvider, verticalPositionProvider, timerTextProvider,
            additionalInfoPositionProvider, additionalInfoTextProvider));
    }

    public void Stop(bool clearDisplay)
    {
        if (_timerCoroutine != null)
        {
            CoroutineRunner.Stop(_timerCoroutine);
            _timerCoroutine = null;
        }

        if (!clearDisplay)
            return;

        foreach (Player player in Player.List)
        {
            RueDisplay display = RueDisplay.Get(player);
            display.SetVisible(_lobbyTimerTag, false);
            display.SetVisible(_additionalInfoTag, false);
            display.Update();
        }
    }

    private IEnumerator TimerCoroutine(Func<bool> isWaitingProvider, Func<ushort> verticalPositionProvider, Func<string> timerTextProvider,
        Func<ushort> additionalInfoPositionProvider, Func<string> additionalInfoTextProvider)
    {
        while (isWaitingProvider())
        {
            BasicElement element = new(verticalPositionProvider(), timerTextProvider());
            BasicElement additionalInfo = new(additionalInfoPositionProvider(), additionalInfoTextProvider());

            foreach (Player player in Player.List)
            {
                RueDisplay display = RueDisplay.Get(player);
                display.Show(_lobbyTimerTag, element);
                display.SetVisible(_lobbyTimerTag, player.IsAlive);
                display.Show(_additionalInfoTag, additionalInfo);
                display.SetVisible(_additionalInfoTag, player.IsAlive);
                display.Update();
            }

            yield return new WaitForSeconds(1f);
        }
        // ReSharper disable once IteratorNeverReturns
    }
}

