﻿using Exiled.API.Features;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using UncomplicatedCustomRoles.Manager;
using UncomplicatedCustomRoles.Structures;
using Handler = UncomplicatedCustomRoles.Events.EventHandler;

using PlayerHandler = Exiled.Events.Handlers.Player;
using ServerHandler = Exiled.Events.Handlers.Server;

namespace UncomplicatedCustomRoles
{
    internal class Plugin : Plugin<Config>
    {
        public override string Name => "UncomplicatedCustomRoles";
        public override string Prefix => "UncomplicatedCustomRoles";
        public override string Author => "FoxWorn3365, Dr.Agenda";
        public override Version Version { get; } = new(0, 8, 2);
        public override Version RequiredExiledVersion { get; } = new(8, 2, 0);
        public static Plugin Instance;
        internal Handler Handler;
        public static Dictionary<int, ICustomRole> CustomRoles;
        public static Dictionary<int, int> PlayerRegistry = new();
        public override void OnEnabled()
        {
            Instance = this;

            Handler = new();
            CustomRoles = new();

            ServerHandler.RoundStarted += Handler.OnRoundStarted;
            ServerHandler.RespawningTeam += Handler.OnRespawningTeam;
            PlayerHandler.Died += Handler.OnDied;

            foreach (ICustomRole CustomRole in Config.CustomRoles)
            {
                SpawnManager.RegisterCustomSubclass(CustomRole);
            }

            base.OnEnabled();
        }
        public override void OnDisabled()
        {
            Instance = null;

            ServerHandler.RoundStarted -= Handler.OnRoundStarted;
            ServerHandler.RespawningTeam -= Handler.OnRespawningTeam;
            PlayerHandler.Died -= Handler.OnDied;

            Handler = null;
            CustomRoles = null;

            base.OnDisabled();
        }
    }
}