using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncomplicatedCustomRoles.Elements;
using UncomplicatedCustomRoles.Manager;
using UncomplicatedCustomRoles.Structures;
using MEC;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Player;
using PlayerRoles;
using Exiled.Events.EventArgs.Scp330;
using UnityEngine;

namespace UncomplicatedCustomRoles.Events
{
    public class EventHandler
    {
        public int CDRole;
        public int SCRole;
        public int FGRole;
        public int CIRole;
        public int Captain;
        public int Specialist;
        public int Sergeant;
        public int Private;


        public void OnRoundStarted()
        {
            // Check for all subclasses and all spawn percentage
            Dictionary<RoleTypeId, List<ICustomRole>> RolePercentage = Factory.RoleIstance();
            foreach (KeyValuePair<int, ICustomRole> Role in Plugin.CustomRoles)
            {
                if (!Role.Value.IgnoreSpawnSystem && Role.Value.SpawnCondition == SpawnCondition.RoundStart && Player.List.Count() >= Role.Value.MinPlayers)
                {
                    foreach (RoleTypeId RoleType in Role.Value.CanReplaceRoles)
                    {
                        for (int a = 0; a < Role.Value.SpawnChance; a++)
                        {
                            RolePercentage[RoleType].Add(Role.Value);
                        }
                    }
                }
            }

            foreach (ICustomRole Role in Plugin.CustomRoles.Values) {
                switch (Role.Role)
                {
                    case RoleTypeId.ClassD:
                        CDRole += 1;
                        break;
                    case RoleTypeId.Scientist:
                        SCRole += 1;
                        break;
                    case RoleTypeId.FacilityGuard:
                        FGRole += 1;
                        break;
                    case RoleTypeId.NtfPrivate:
                        Private += 1;
                        break;
                    case RoleTypeId.NtfSergeant:
                        Sergeant += 1;
                        break;
                    case RoleTypeId.NtfSpecialist:
                        Specialist += 1;
                        break;
                    case RoleTypeId.NtfCaptain:
                        Captain += 1;
                        break;

                }
            }

            // Now check all the player list and assign a custom subclasses for every role
            foreach (Player Player in Player.List)
            {
                if (RolePercentage.ContainsKey(Player.Role.Type))
                {
                    // We can proceed with the chance
                    int Chance = new System.Random().Next(0, 100);
                    if (Chance < RolePercentage[Player.Role.Type].Count())
                    {
                        // The role exists, good, let's give the player a role
                        int RoleId = RolePercentage[Player.Role.Type].RandomItem().Id;

                        Timing.RunCoroutine(DoSpawnPlayer(Player, RoleId, RolePercentage, false));
                    }
                }
            }
        }
        public void OnRespawningTeam(RespawningTeamEventArgs Respawn)
        {
            Log.Debug("Respawning event, reset the queue");
            Plugin.RoleSpawnQueue.Clear();
            
            foreach (Player Player in Respawn.Players.ToList())
            {
                Plugin.RoleSpawnQueue.Add(Player.Id);
                Log.Debug($"Player {Player.Nickname} queued for spawning as CustomRole, will be define when spawned");
            }
        }
        public void OnPlayerSpawned(SpawnedEventArgs Spawned)
        {
            Log.Debug($"Player {Spawned.Player.Nickname} spawned, going to assign a role if needed!");
            Timing.CallDelayed(0.2f, () =>
            {
                if (Plugin.RoleSpawnQueue.Contains(Spawned.Player.Id))
                {
                    Log.Debug($"Assigning a role to {Spawned.Player.Nickname}");
                    Plugin.RoleSpawnQueue.Remove(Spawned.Player.Id);
                    Timing.RunCoroutine(DoElaborateSpawnPlayerFromWave(Spawned.Player, true));
                    Log.Debug($"Player {Spawned.Player.Nickname} successfully spawned as CustomRole {Plugin.RoleSpawnQueue[Spawned.Player.Id]}");
                }
                else
                {
                    ICustomRole? c = API.Features.Manager.Get(Spawned.Player);
                    if (c.Abilities.Contains(Ability.InfSprint))
                    {
                        Spawned.Player.IsUsingStamina = true;
                    }

                    Plugin.PlayerRegistry.Remove(Spawned.Player.Id);
                    Spawned.Player.CustomInfo = "";

                }
                Log.Debug(Plugin.RoleSpawnQueue.Count().ToString());
            });
        }


        public void OnDied(DiedEventArgs Died)
        {
            if (Died.Attacker != null && Plugin.PlayerRegistry.ContainsKey(Died.Attacker.Id))
            {
                ICustomRole? c = API.Features.Manager.Get(Died.Attacker);
                if (c.Abilities.Contains(Ability.KillEqualsInvisi))
                {
                    Died.Attacker.EnableEffect(Exiled.API.Enums.EffectType.Invisible, 5, true);
                }
                if (c.Abilities.Contains(Ability.KillEqualsAmmo))
                {
                    Died.Attacker.AddAmmo(Exiled.API.Enums.AmmoType.Nato556, 10);
                    Died.Attacker.AddAmmo(Exiled.API.Enums.AmmoType.Nato762, 10);
                    Died.Attacker.AddAmmo(Exiled.API.Enums.AmmoType.Nato9, 10);
                    Died.Attacker.AddAmmo(Exiled.API.Enums.AmmoType.Ammo44Cal, 4);
                    Died.Attacker.AddAmmo(Exiled.API.Enums.AmmoType.Ammo12Gauge, 6);

                }
            }

            if (Plugin.PlayerRegistry.ContainsKey(Died.Player.Id))
            {
                ICustomRole? c = API.Features.Manager.Get(Died.Player);
                if (c.Abilities.Contains(Ability.InfSprint))
                {
                    Died.Player.IsUsingStamina = true;
                }
                Plugin.RolesCount[Plugin.PlayerRegistry[Died.Player.Id]]--;
                Plugin.PlayerRegistry.Remove(Died.Player.Id);
                Died.Player.CustomInfo = "";
                Died.Player.UniqueRole = "";
                
                // Died.Player.Group = new UserGroup();
            }
            Died.Player.Scale = new Vector3(1, 1, 1);
        }

        public void InteractingWith330(InteractingScp330EventArgs ev)
        {
            ICustomRole? c = API.Features.Manager.Get(ev.Player);
            if(c != null) {
                if(c.Abilities.Contains(Ability.MoreCandy))
                {
                    if(ev.UsageCount <= 2)
                    {
                        ev.ShouldSever = false;
                    }
                    else if(ev.UsageCount > 2)
                    {
                        ev.ShouldSever = true;
                    }
                }
            }
        }

        public void OnRoundRestart()
        {
            Plugin.PlayerRegistry.Clear();
            Plugin.RolesCount.Clear();
            Plugin.CustomRoles.Clear();
            CDRole = 0;
            SCRole = 0;
            FGRole = 0;
            CIRole = 0;
            Captain = 0;
            Specialist = 0;
            Sergeant = 0;
            Private = 0;
            foreach (ICustomRole CustomRole in Plugin.Instance.Config.CustomRoles)
            {
                SpawnManager.RegisterCustomSubclass(CustomRole);
            }

        }
        public static IEnumerator<float> DoSpawnPlayer(Player Player, int Id, Dictionary<RoleTypeId, List<ICustomRole>> RolePercentage, bool DoBypassRoleOverwrite = true)
        {

            yield return Timing.WaitForSeconds(0.1f);

            if (Plugin.RolesCount[Id] < Plugin.CustomRoles[Id].MaxPlayers)
            {
                SpawnManager.SummonCustomSubclass(Player, Id, DoBypassRoleOverwrite);
                Plugin.RolesCount[Id]++;
                Log.Debug($"Player {Player.Nickname} spawned as CustomRole {Id}");
            }
            else
            {
              int RID = RolePercentage[Player.Role.Type].RandomItem().Id;

              Timing.RunCoroutine(DoSpawnPlayer(Player, RID, RolePercentage, false));

            }

        }

        public static IEnumerator<float> DoSpawnPlayer(Player Player, int Id, bool DoBypassRoleOverwrite = true)
        {

                yield return Timing.WaitForSeconds(0.1f);
                SpawnManager.SummonCustomSubclass(Player, Id, DoBypassRoleOverwrite);
        }
        public static IEnumerator<float> DoElaborateSpawnPlayerFromWave(Player Player, bool DoBypassRoleOverwrite = true)
        {
            yield return Timing.WaitForSeconds(0.1f);
            Dictionary<RoleTypeId, List<ICustomRole>> RolePercentage = Factory.RoleIstance();

            foreach (KeyValuePair<int, ICustomRole> Role in Plugin.CustomRoles)
            {
                if (!Role.Value.IgnoreSpawnSystem && Role.Value.CanReplaceRoles.Contains(Player.Role.Type) && Role.Value.MaxPlayers > Plugin.RolesCount[Role.Value.Id] && Role.Value.MinPlayers >= Player.List.Count())
                {
                    foreach (RoleTypeId RoleType in Role.Value.CanReplaceRoles)
                    {
                        for (int a = 0; a < Role.Value.SpawnChance; a++)
                        {
                            RolePercentage[RoleType].Add(Role.Value);
                        }
                    }
                }
            }
            int Chance = new System.Random().Next(0, 100);
            if (Chance >= RolePercentage.Count())
            {
                yield break;
            }
            int RoleId = RolePercentage[Player.Role.Type].RandomItem().Id;
            Plugin.RolesCount[RoleId]++;
            Timing.RunCoroutine(DoSpawnPlayer(Player, RoleId, RolePercentage, false));
            yield break;
        }
    }
}