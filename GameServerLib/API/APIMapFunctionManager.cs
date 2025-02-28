﻿using Force.Crc32;
using GameServerCore.Domain;
using GameServerCore.Domain.GameObjects;
using GameServerCore.Enums;
using GameServerLib.GameObjects;
using LeaguePackets.Game.Common;
using LeagueSandbox.GameServer.GameObjects;
using LeagueSandbox.GameServer.GameObjects.AttackableUnits.AI;
using LeagueSandbox.GameServer.GameObjects.AttackableUnits.Buildings.AnimatedBuildings;
using LeagueSandbox.GameServer.Logging;
using LeagueSandbox.GameServer.Maps;
using log4net;
using PacketDefinitions420;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using static MapData;

namespace LeagueSandbox.GameServer.API
{
    public static class ApiMapFunctionManager
    {
        // Required variables.
        private static Game _game;
        private static ILog _logger;
        private static MapScriptHandler _map;

        /// <summary>
        /// Sets the Game instance of ApiFunctionManager to the given instance.
        /// Also assigns the debug logger.
        /// </summary>
        /// <param name="game">Game instance to set.</param>
        internal static void SetMap(Game game, MapScriptHandler mapScriptHandler)
        {
            _game = game;
            _map = mapScriptHandler;
            _logger = LoggerProvider.GetLogger();
        }

        public static void AddProtection(IAttackableUnit unit, IAttackableUnit[] dependOnAll, IAttackableUnit[] dependOnSingle)
        {
            _game.ProtectionManager.AddProtection(unit, dependOnAll, dependOnSingle);
        }

        public static void AddProtection(IAttackableUnit unit, bool dependOnAll, params IAttackableUnit[] dependOn)
        {
            _game.ProtectionManager.AddProtection(unit, dependOnAll, dependOn);
        }

        public static IGameObject CreateShop(string name, Vector2 position, TeamId team)
        {
            return new GameObject(_game, position, team: team, netId: Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(name)) | 0xFF000000);
        }

        /// <summary>
        /// Creates and returns a nexus
        /// </summary>
        /// <param name="name"></param>
        /// <param name="model"></param>
        /// <param name="position"></param>
        /// <param name="team"></param>
        /// <param name="nexusRadius"></param>
        /// <param name="sightRange"></param>
        /// <returns></returns>
        public static INexus CreateNexus(string name, string model, Vector2 position, TeamId team, int nexusRadius, int sightRange)
        {
            return new Nexus(_game, model, team, nexusRadius, position, sightRange, Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(name)) | 0xFF000000);
        }

        /// <summary>
        /// Creates and returns an inhibitor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="model"></param>
        /// <param name="position"></param>
        /// <param name="team"></param>
        /// <param name="lane"></param>
        /// <param name="inhibRadius"></param>
        /// <param name="sightRange"></param>
        /// <returns></returns>
        public static IInhibitor CreateInhibitor(string name, string model, Vector2 position, TeamId team, LaneID lane, int inhibRadius, int sightRange)
        {
            return new Inhibitor(_game, model, lane, team, inhibRadius, position, sightRange, Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(name)) | 0xFF000000);
        }

        public static MapObject CreateLaneMinionSpawnPos(string name, Vector3 position)
        {
            return new MapObject(name, position, _map.Id);
        }

        /// <summary>
        /// Creates a tower
        /// </summary>
        /// <param name="name"></param>
        /// <param name="model"></param>
        /// <param name="position"></param>
        /// <param name="team"></param>
        /// <param name="turretType"></param>
        /// <param name="turretItems"></param>
        /// <param name="lane"></param>
        /// <param name="aiScript"></param>
        /// <param name="mapObject"></param>
        /// <param name="netId"></param>
        /// <returns></returns>
        public static ILaneTurret CreateLaneTurret(string name, string model, Vector2 position, TeamId team, TurretType turretType, LaneID lane, string aiScript, MapObject mapObject = default, uint netId = 0)
        {
            return new LaneTurret(_game, name, model, position, team, turretType, netId, lane, mapObject, aiScript);
        }

        /// <summary>
        /// Gets the turret item list from MapScripts
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int[] GetTurretItems(Dictionary<TurretType, int[]> turretItemList, TurretType type)
        {
            if (!turretItemList.ContainsKey(type))
            {
                return new int[] { };
            }

            return turretItemList[type];
        }

        /// <summary>
        /// Changes the lane of a tower in the _tower list, in order to fix missplaced towers due to riot's seemingly random naming scheme
        /// </summary>
        /// <param name="towerName"></param>
        /// <param name="team"></param>
        /// <param name="currentLaneId"></param>
        /// <param name="desiredLaneID"></param>
        public static void ChangeTowerOnMapList(Dictionary<TeamId, Dictionary<LaneID, List<ILaneTurret>>> turretList, string towerName, TeamId team, LaneID currentLaneId, LaneID desiredLaneID)
        {
            var tower = turretList[team][currentLaneId].Find(x => x.Name == towerName);
            tower.SetLaneID(desiredLaneID);
            turretList[team][currentLaneId].Remove(tower);
            turretList[team][desiredLaneID].Add(tower);
        }

        public static IInhibitor GetInhibitorById(Dictionary<TeamId, Dictionary<LaneID, IInhibitor>> inhibitorList, uint id)
        {
            foreach (TeamId team in inhibitorList.Keys)
            {
                foreach (var inhibitor in inhibitorList[team].Values)
                {
                    if (inhibitor.NetId == id)
                    {
                        if (inhibitor.NetId == id)
                        {
                            return inhibitor;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Checks is all inhibitors of a specific team are dead
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public static bool AllInhibitorsDestroyedFromTeam(Dictionary<TeamId, Dictionary<LaneID, IInhibitor>> inhibitorList, TeamId team)
        {
            foreach (var inhibitor in inhibitorList[team].Values)
            {
                if (inhibitor.Team == team && inhibitor.InhibitorState == InhibitorState.ALIVE)
                {
                    if (inhibitor.Team == team && inhibitor.InhibitorState == InhibitorState.ALIVE)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Spawns a LaneMinion
        /// </summary>
        /// <param name="list"></param>
        /// <param name="minionNo"></param>
        /// <param name="barracksName"></param>
        /// <param name="waypoints"></param>
        public static void CreateLaneMinion(List<MinionSpawnType> list, Vector2 position, TeamId team, int minionNo, string barracksName, List<Vector2> waypoints)
        {
            if (list.Count <= minionNo)
            {
                return;
            }

            var m = new LaneMinion(_game, list[minionNo], position, barracksName, waypoints, _map.MapScript.MinionModels[team][list[minionNo]], 0, team, _map.MapScript.LaneMinionAI);
            _game.ObjectManager.AddObject(m);
        }

        /// <summary>
        /// Creates and returns a minion
        /// </summary>
        /// <param name="name"></param>
        /// <param name="model"></param>
        /// <param name="position"></param>
        /// <param name="netId"></param>
        /// <param name="team"></param>
        /// <param name="skinId"></param>
        /// <param name="ignoreCollision"></param>
        /// <param name="isTargetable"></param>
        /// <param name="aiScript"></param>
        /// <param name="damageBonus"></param>
        /// <param name="healthBonus"></param>
        /// <param name="initialLevel"></param>
        /// <returns></returns>
        public static IMinion CreateMinion(
            string name, string model, Vector2 position, uint netId = 0,
            TeamId team = TeamId.TEAM_NEUTRAL, int skinId = 0, bool ignoreCollision = false,
            bool isTargetable = false, string aiScript = "", int damageBonus = 0,
            int healthBonus = 0, int initialLevel = 1)
        {
            var m = new Minion(_game, null, position, model, name, netId, team, skinId, ignoreCollision, isTargetable, null, aiScript, damageBonus, healthBonus, initialLevel);
            _game.ObjectManager.AddObject(m);
            return m;
        }

        /// <summary>
        /// Checks if minion spawn is enabled
        /// </summary>
        /// <returns></returns>
        public static bool IsMinionSpawnEnabled()
        {
            return _game.Config.GameFeatures.HasFlag(FeatureFlags.EnableLaneMinions);
        }

        /// <summary>
        /// Creates and returns a jungle camp
        /// </summary>
        /// <param name="position"></param>
        /// <param name="groupNumber"></param>
        /// <param name="teamSideOfTheMap"></param>
        /// <param name="campTypeIcon"></param>
        /// <param name="respawnTimer"></param>
        /// <param name="doPlayVO"></param>
        /// <param name="revealEvent"></param>
        /// <param name="spawnDuration"></param>
        /// <returns></returns>
        public static IMonsterCamp CreateJungleCamp(Vector3 position, byte groupNumber, TeamId teamSideOfTheMap, string campTypeIcon, float respawnTimer, bool doPlayVO = false, byte revealEvent = 74, float spawnDuration = 0.0f)
        {
            return new MonsterCamp(_game, position, groupNumber, teamSideOfTheMap, campTypeIcon, respawnTimer, doPlayVO, revealEvent, spawnDuration);
        }

        /// <summary>
        /// Creates and returns a jungle monster
        /// </summary>
        /// <param name="name"></param>
        /// <param name="model"></param>
        /// <param name="position"></param>
        /// <param name="faceDirection"></param>
        /// <param name="monsterCamp"></param>
        /// <param name="team"></param>
        /// <param name="spawnAnimation"></param>
        /// <param name="netId"></param>
        /// <param name="isTargetable"></param>
        /// <param name="ignoresCollision"></param>
        /// <param name="aiScript"></param>
        /// <param name="damageBonus"></param>
        /// <param name="healthBonus"></param>
        /// <param name="initialLevel"></param>
        public static IMonster CreateJungleMonster
        (
            string name, string model, Vector2 position, Vector3 faceDirection,
            IMonsterCamp monsterCamp, TeamId team = TeamId.TEAM_NEUTRAL, string spawnAnimation = "", uint netId = 0,
            bool isTargetable = true, bool ignoresCollision = false, string aiScript = "",
            int damageBonus = 0, int healthBonus = 0, int initialLevel = 1
        )
        {
            return new Monster(_game, name, model, position, faceDirection, monsterCamp, team, netId, spawnAnimation, isTargetable, ignoresCollision, aiScript, damageBonus, healthBonus, initialLevel);
        }

        /// <summary>
        /// Set an unit's icon on minimap
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="iconCategory"></param>
        /// <param name="changeIcon"></param>
        /// <param name="borderCategory"></param>
        /// <param name="changeBorder"></param>
        public static void SetMinimapIcon(IAttackableUnit unit, string iconCategory = "", bool changeIcon = false, string borderCategory = "", bool changeBorder = false)
        {
            _game.PacketNotifier.NotifyS2C_UnitSetMinimapIcon(unit, iconCategory, changeIcon, borderCategory, changeBorder);
        }

        /// <summary>
        /// Adds a prop to the map
        /// </summary>
        /// <param name="name"></param>
        /// <param name="model"></param>
        /// <param name="position"></param>
        /// <param name="height"></param>
        /// <param name="direction"></param>
        /// <param name="posOffset"></param>
        /// <param name="scale"></param>
        /// <param name="skinId"></param>
        /// <param name="skillLevel"></param>
        /// <param name="rank"></param>
        /// <param name="type"></param>
        /// <param name="netId"></param>
        /// <param name="netNodeId"></param>
        /// <returns></returns>
        public static ILevelProp AddLevelProp(string name, string model, Vector3 position, Vector3 direction, Vector3 posOffset, Vector3 scale, int skinId = 0, byte skillLevel = 0, byte rank = 0, byte type = 2, uint netId = 0, byte netNodeId = 64)
        {
            var prop = new LevelProp(_game, netNodeId, name, model, position, direction, posOffset, scale, skinId, skillLevel, rank, type, netId);
            _game.ObjectManager.AddObject(prop);
            return prop;
        }

        /// <summary>
        /// Notifies prop animations (Such as the stairs at the beginning on Dominion)
        /// </summary>
        /// <param name="prop"></param>
        /// <param name="animation"></param>
        /// <param name="animationFlag"></param>
        /// <param name="duration"></param>
        /// <param name="destroyPropAfterAnimation"></param>
        public static void NotifyPropAnimation(ILevelProp prop, string animation, AnimationFlags animationFlag, float duration, bool destroyPropAfterAnimation)
        {
            var animationData = new UpdateLevelPropDataPlayAnimation
            {
                AnimationName = animation,
                AnimationFlags = (uint)animationFlag,
                Duration = duration,
                DestroyPropAfterAnimation = destroyPropAfterAnimation,
                StartMissionTime = _game.GameTime,
                NetID = prop.NetId
            };
            _game.PacketNotifier.NotifyUpdateLevelPropS2C(animationData);
        }

        /// <summary>
        /// Sets up the surrender functionality
        /// </summary>
        /// <param name="time"></param>
        /// <param name="restTime"></param>
        /// <param name="length"></param>
        public static void AddSurrender(float time, float restTime, float length)
        {
            _map.Surrenders.Add(TeamId.TEAM_BLUE, new SurrenderHandler(_game, TeamId.TEAM_BLUE, time, restTime, length));
            _map.Surrenders.Add(TeamId.TEAM_PURPLE, new SurrenderHandler(_game, TeamId.TEAM_PURPLE, time, restTime, length));
        }

        public static void HandleSurrender(int userId, IChampion who, bool vote)
        {
            if (_map.Surrenders.ContainsKey(who.Team))
                _map.Surrenders[who.Team].HandleSurrender(userId, who, vote);
        }

        /// <summary>
        /// Adds a fountain
        /// </summary>
        /// <param name="team"></param>
        /// <param name="position"></param>
        public static IFountain CreateFountain(TeamId team, Vector2 position, float radius = 1000.0f)
        {
            return new Fountain(_game, team, position, radius);
        }

        /// <summary>
        /// Sets the features which should be enabled for this map. EX: Mana, Cooldowns, Lane Minions, etc. Refer to FeatureFlags enum.
        /// </summary>
        /// <returns></returns>
        public static void SetGameFeatures(FeatureFlags featureFlag, bool isEnabled)
        {
            _game.Config.SetGameFeatures(featureFlag, isEnabled);
        }

        /// <summary>
        /// Announces an event
        /// </summary>
        /// <param name="Event"></param>
        /// <param name="mapId"></param>
        public static void NotifyMapAnnouncement(GameServerCore.Enums.EventID Event, int mapId = 0)
        {
            _game.PacketNotifier.NotifyS2C_OnEventWorld(PacketExtensions.GetAnnouncementID(Event, mapId));
        }

        /// <summary>
        /// Returns current game time.
        /// </summary>
        /// <returns></returns>
        public static float GameTime()
        {
            return _game.GameTime;
        }

        /// <summary>
        /// Sets the game to exit
        /// </summary>
        /// <param name="losingTeam">The team who lost the game</param>
        /// <param name="finalCameraPosition">The position which the camera has to move to for the end-game screen</param>
        /// <param name="endGameTimer">Offset for the Endgame screend (victory or defeat) to be actually announced</param>
        /// <param name="moveCamera">Wether or not the camera should move</param>
        /// <param name="cameraTimer">The ammount of time the camera has to arrive to it's destination</param>
        /// <param name="disableUI">Whether or not the UI should get disabled</param>
        /// <param name="deathData">DeathData of what triggered the End of the Game, such as necus death</param>
        public static void EndGame(TeamId losingTeam, Vector3 finalCameraPosition, float endGameTimer = 5000.0f, bool moveCamera = true, float cameraTimer = 3.0f, bool disableUI = true, IDeathData deathData = null)
        {
            //TODO: check if mapScripts should handle this directly
            var players = _game.PlayerManager.GetPlayers();
            _game.Stop();

            if (deathData != null)
            {
                _game.PacketNotifier.NotifyBuilding_Die(deathData);
                _game.PacketNotifier.NotifyS2C_SetGreyscaleEnabledWhenDead(false, deathData.Killer);
            }
            else
            {
                _game.PacketNotifier.NotifyS2C_SetGreyscaleEnabledWhenDead(false);
            }

            _game.PacketNotifier.NotifyS2C_EndGame(losingTeam, endGameTimer);
            foreach (var player in players)
            {
                if (disableUI)
                {
                    _game.PacketNotifier.NotifyS2C_DisableHUDForEndOfGame(player);
                }
                if (moveCamera)
                {
                    _game.PacketNotifier.NotifyS2C_MoveCameraToPoint(player, Vector3.Zero, finalCameraPosition, cameraTimer);
                }
            }
            _game.SetGameToExit();
        }

        public static void AddTurretItems(IBaseTurret turret, int[] items)
        {
            foreach (var item in items)
            {
                turret.Inventory.AddItem(_game.ItemManager.SafeGetItemType(item), turret);
            }
        }

        public static void NotifySpawn(IGameObject obj)
        {
            _game.ObjectManager.SpawnObject(obj);
        }

        public static void AddObject(IGameObject obj)
        {
            _game.ObjectManager.AddObject(obj);
        }
    }
}
