using System.ComponentModel;
using Exiled.API.Interfaces;
using JustWaitingLobby.Handlers;

namespace JustWaitingLobby
{
    public sealed class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        [Description("Блок с баннером сервера")]
        public LobbyHandler LobbyHandler { get; set; } = new();
    }
}