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
    private Coroutine _timerCoroutine;

    public void Start(bool showLobbyTimer, Func<bool> isWaitingProvider, Func<ushort> verticalPositionProvider, Func<string> timerTextProvider)
    {
        Stop(clearDisplay: false);

        if (!showLobbyTimer)
            return;

        _timerCoroutine = CoroutineRunner.Run(TimerCoroutine(isWaitingProvider, verticalPositionProvider, timerTextProvider));
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
            display.Update();
        }
    }

    private IEnumerator TimerCoroutine(Func<bool> isWaitingProvider, Func<ushort> verticalPositionProvider, Func<string> timerTextProvider)
    {
        while (isWaitingProvider())
        {
            BasicElement element = new(verticalPositionProvider(), timerTextProvider());

            foreach (Player player in Player.List)
            {
                RueDisplay display = RueDisplay.Get(player);
                display.Show(_lobbyTimerTag, element);
                display.SetVisible(_lobbyTimerTag, true);
                display.Update();
            }

            yield return new WaitForSeconds(1f);
        }
        // ReSharper disable once IteratorNeverReturns
    }
}

