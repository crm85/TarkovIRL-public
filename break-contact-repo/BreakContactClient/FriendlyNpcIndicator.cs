using BreakContact.Configs;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BreakContact
{
    internal sealed class FriendlyNpcIndicator : MonoBehaviour
    {
        private const float DotTopOffset = 0.18f;
        private const float FallbackDotHeight = 2.0f;
        private const float DebugLogInterval = 5f;
        private static readonly Color FriendlyDotColor = new Color(0.45f, 0.82f, 1f, 1f);

        private static FriendlyNpcIndicator _instance;
        private static ManualLogSource _logger;
        private static readonly FriendlyPmcBridge _friendlyPmcBridge = new FriendlyPmcBridge();

        private readonly Dictionary<string, FriendlyNpcDot> _dots = new Dictionary<string, FriendlyNpcDot>();
        private readonly HashSet<string> _candidateIds = new HashSet<string>();
        private readonly List<Player> _playerCandidates = new List<Player>();
        private readonly HashSet<string> _seenIds = new HashSet<string>();
        private float _nextDebugLogTime;

        public static void SetLogger(ManualLogSource logger)
        {
            _logger = logger;
        }

        public static void Ensure()
        {
            if (!BreakContactConfig.EnableFriendlyNpcIndicator.Value) return;
            if (_instance != null) return;

            GameObject managerObject = new GameObject("BreakContactFriendlyNpcIndicator");
            DontDestroyOnLoad(managerObject);
            _instance = managerObject.AddComponent<FriendlyNpcIndicator>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Update()
        {
            if (!BreakContactConfig.EnableFriendlyNpcIndicator.Value || !BreakContactPlugin.InGame())
            {
                ClearDots();
                return;
            }

            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            Player localPlayer = gameWorld?.MainPlayer;
            Camera camera = ScreenSpaceDotResources.ResolvePlayerCamera();
            if (gameWorld == null || localPlayer == null || camera == null)
            {
                HideDots();
                return;
            }

            int maxDistance = Mathf.Clamp(BreakContactConfig.FriendlyNpcIndicatorMaxDistance.Value, 1, 100);
            _seenIds.Clear();
            bool debugLog = BreakContactConfig.FriendlyNpcIndicatorDebugLogging.Value && Time.time >= _nextDebugLogTime;
            int totalPlayers = 0;
            int aiPmcPlayers = 0;
            int friendlyPlayers = 0;
            int visibleDots = 0;
            List<string> debugLines = debugLog ? new List<string>() : null;

            BuildPlayerCandidates(gameWorld);
            for (int i = 0; i < _playerCandidates.Count; i++)
            {
                Player npc = _playerCandidates[i];
                totalPlayers++;
                bool npcIsAiPmc = IsAiPmc(npc);
                string reason;
                if (!IsFriendlyNpc(localPlayer, npc, out reason))
                {
                    if (npcIsAiPmc)
                    {
                        aiPmcPlayers++;
                        if (debugLog)
                            AddDebugLine(debugLines, localPlayer, npc, reason, maxDistance);
                    }

                    continue;
                }

                if (npcIsAiPmc)
                    aiPmcPlayers++;

                friendlyPlayers++;

                string trackingId = GetTrackingId(npc);
                _seenIds.Add(trackingId);

                FriendlyNpcDot dot;
                if (!_dots.TryGetValue(trackingId, out dot))
                {
                    dot = new FriendlyNpcDot(npc);
                    _dots[trackingId] = dot;
                }

                if (dot.Update(localPlayer, camera, maxDistance))
                    visibleDots++;

                if (debugLog)
                    AddDebugLine(debugLines, localPlayer, npc, "friendly:" + reason, maxDistance);
            }

            if (debugLog)
            {
                _nextDebugLogTime = Time.time + DebugLogInterval;
                _logger?.LogWarning("[FriendlyNpcIndicator] localSide=" + localPlayer.Side
                    + " localGroup=" + SafeText(localPlayer.GroupId)
                    + " totalPlayers=" + totalPlayers
                    + " aiPmcPlayers=" + aiPmcPlayers
                    + " friendlyPlayers=" + friendlyPlayers
                    + " visibleDots=" + visibleDots
                    + " staleDotsBeforePrune=" + _dots.Count
                    + " camera=" + camera.name);

                for (int i = 0; i < debugLines.Count; i++)
                    _logger?.LogWarning("[FriendlyNpcIndicator] " + debugLines[i]);
            }

            RemoveStaleDots();
        }

        private void BuildPlayerCandidates(GameWorld gameWorld)
        {
            _playerCandidates.Clear();
            _candidateIds.Clear();

            if (gameWorld == null) return;

            AddPlayerCandidates(gameWorld.AllAlivePlayersList);
            AddPlayerCandidates(gameWorld.RegisteredPlayers);
        }

        private void AddPlayerCandidates(IEnumerable players)
        {
            if (players == null) return;

            foreach (object value in players)
            {
                Player player = value as Player;
                if (player == null) continue;

                string trackingId = GetTrackingId(player);
                if (!_candidateIds.Add(trackingId)) continue;

                _playerCandidates.Add(player);
            }
        }

        private static bool IsFriendlyNpc(Player localPlayer, Player npc, out string reason)
        {
            reason = "unknown";
            if (localPlayer == null || npc == null)
            {
                reason = "missing-player";
                return false;
            }

            if (npc == localPlayer || npc.IsYourPlayer)
            {
                reason = "local-player";
                return false;
            }

            if (npc.HealthController?.IsAlive != true)
            {
                reason = "dead";
                return false;
            }

            bool botDebugFriendly;
            if (TryGetBotDebugFriendlyState(localPlayer, npc, out botDebugFriendly))
            {
                reason = botDebugFriendly ? "botdebug-not-enemy" : "botdebug-enemy";
                return botDebugFriendly;
            }

            bool isFriendlyPmcSquadMate;
            if (TryGetFriendlyPmcFollower(npc, out isFriendlyPmcSquadMate))
            {
                reason = isFriendlyPmcSquadMate ? "friendlypmc-squad-follower" : "friendlypmc-follower";
                return true;
            }

            if (!string.IsNullOrEmpty(localPlayer.GroupId) && localPlayer.GroupId == npc.GroupId)
            {
                reason = "same-group";
                return true;
            }

            if (!IsPmcSide(localPlayer.Side))
            {
                reason = "local-not-pmc:" + localPlayer.Side;
                return false;
            }

            if (!IsPmcSide(npc.Side))
            {
                reason = "npc-not-pmc:" + npc.Side;
                return false;
            }

            if (!npc.IsAI)
            {
                reason = "npc-not-ai";
                return false;
            }

            BotsGroup group = ResolveBotsGroup(npc);
            if (group != null)
            {
                bool relation;
                bool checkedEnemyRelation = false;

                if (TryInvokeGroupRelation(group, "IsEnemy", localPlayer, out relation))
                {
                    checkedEnemyRelation = true;
                    if (relation)
                    {
                        reason = "group-is-enemy";
                        return false;
                    }
                }

                if (TryInvokeGroupRelation(group, "IsPlayerEnemy", localPlayer, out relation))
                {
                    checkedEnemyRelation = true;
                    if (relation)
                    {
                        reason = "group-is-player-enemy";
                        return false;
                    }
                }

                if (TryInvokeGroupRelation(group, "IsAlly", localPlayer, out relation) && relation)
                {
                    reason = "group-is-ally";
                    return true;
                }

                if (checkedEnemyRelation)
                {
                    reason = "group-not-enemy";
                    return true;
                }

                reason = "group-relation-unavailable";
            }
            else
            {
                reason = "no-bots-group";
            }

            if (_friendlyPmcBridge.IsSameSideFriendlyEnabled() && localPlayer.Side == npc.Side)
            {
                reason = "friendlypmc-same-side";
                return true;
            }

            return false;
        }

        private static bool TryGetBotDebugFriendlyState(Player localPlayer, Player npc, out bool isFriendly)
        {
            isFriendly = false;

            BotOwner botOwner = ResolveBotOwner(npc);
            if (localPlayer == null || botOwner?.EnemiesController?.EnemyInfos == null)
                return false;

            isFriendly = true;

            try
            {
                IEnumerable enemyInfos = botOwner.EnemiesController.EnemyInfos.Values;
                foreach (object enemyInfo in enemyInfos)
                {
                    string enemyProfileId = ReadStringMember(enemyInfo, "ProfileId");
                    if (string.IsNullOrEmpty(enemyProfileId)) continue;

                    if (enemyProfileId == localPlayer.ProfileId)
                    {
                        isFriendly = false;
                        break;
                    }
                }

                return true;
            }
            catch
            {
                isFriendly = false;
                return false;
            }
        }

        private static string ReadStringMember(object source, string memberName)
        {
            if (source == null || string.IsNullOrEmpty(memberName)) return null;

            Type type = source.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            while (type != null)
            {
                PropertyInfo property = type.GetProperty(memberName, flags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    object value = property.GetValue(source, null);
                    return value as string;
                }

                FieldInfo field = type.GetField(memberName, flags);
                if (field != null)
                {
                    object value = field.GetValue(source);
                    return value as string;
                }

                type = type.BaseType;
            }

            return null;
        }

        private static bool IsPmcSide(EPlayerSide side)
        {
            return side == EPlayerSide.Usec || side == EPlayerSide.Bear;
        }

        private static BotsGroup ResolveBotsGroup(Player player)
        {
            BotOwner botOwner = ResolveBotOwner(player);
            return player?.BotsGroup ?? botOwner?.BotsGroup;
        }

        private static BotOwner ResolveBotOwner(Player player)
        {
            return player?.AIData?.BotOwner;
        }

        private static bool TryGetFriendlyPmcFollower(Player player, out bool isSquadMate)
        {
            return _friendlyPmcBridge.TryGetFollower(ResolveBotOwner(player), out isSquadMate);
        }

        private static bool IsAiPmc(Player player)
        {
            return player != null && player.IsAI && IsPmcSide(player.Side);
        }

        private static void AddDebugLine(List<string> lines, Player localPlayer, Player npc, string reason, int maxDistance)
        {
            if (lines == null || lines.Count >= 12 || npc == null) return;

            float distance = localPlayer != null ? Vector3.Distance(localPlayer.Position, npc.Position) : -1f;
            lines.Add("candidate profile=" + SafeText(npc.ProfileId)
                + " side=" + npc.Side
                + " group=" + SafeText(npc.GroupId)
                + " isAI=" + npc.IsAI
                + " hasGroup=" + (ResolveBotsGroup(npc) != null)
                + " distance=" + distance.ToString("0.0")
                + "/" + maxDistance
                + " reason=" + reason);
        }

        private static string SafeText(string text)
        {
            return string.IsNullOrEmpty(text) ? "<empty>" : text;
        }

        private static bool TryInvokeGroupRelation(BotsGroup group, string methodName, Player player, out bool relation)
        {
            relation = false;
            if (group == null || player == null) return false;

            try
            {
                MethodInfo method = null;
                MethodInfo[] methods = group.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo candidate = methods[i];
                    if (candidate.Name != methodName || candidate.ReturnType != typeof(bool)) continue;

                    ParameterInfo[] parameters = candidate.GetParameters();
                    if (parameters.Length != 1 || !parameters[0].ParameterType.IsInstanceOfType(player)) continue;

                    method = candidate;
                    break;
                }

                object result = method?.Invoke(group, new object[] { player });
                if (result is bool boolResult)
                {
                    relation = boolResult;
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private sealed class FriendlyPmcBridge
        {
            private const float ResolveRetryInterval = 5f;

            private Type _bossPlayersType;
            private Type _friendlyPmcType;
            private MethodInfo _getFollowersMethod;
            private MethodInfo _getBotMethod;
            private PropertyInfo _isSquadMateProperty;
            private FieldInfo _friendlyPmcFlagField;
            private FieldInfo _badGuyField;
            private PropertyInfo _configEntryValueProperty;
            private float _nextResolveTime;
            private bool _available;

            public bool TryGetFollower(BotOwner botOwner, out bool isSquadMate)
            {
                isSquadMate = false;
                if (botOwner == null || !EnsureAvailable()) return false;

                try
                {
                    IEnumerable followers = _getFollowersMethod?.Invoke(null, null) as IEnumerable;
                    if (followers == null) return false;

                    foreach (object follower in followers)
                    {
                        if (follower == null) continue;

                        object followerBot = _getBotMethod?.Invoke(follower, null);
                        if (!ReferenceEquals(followerBot, botOwner)) continue;

                        object rawSquadMate = _isSquadMateProperty?.GetValue(follower, null);
                        if (rawSquadMate is bool squadMate)
                            isSquadMate = squadMate;

                        return true;
                    }
                }
                catch
                {
                    _available = false;
                    _nextResolveTime = Time.unscaledTime + ResolveRetryInterval;
                }

                return false;
            }

            public bool IsSameSideFriendlyEnabled()
            {
                if (!EnsureAvailable()) return false;

                return ReadConfigBool(_friendlyPmcFlagField, false)
                    && !ReadConfigBool(_badGuyField, false);
            }

            private bool EnsureAvailable()
            {
                if (_available) return true;
                if (Time.unscaledTime < _nextResolveTime) return false;

                _nextResolveTime = Time.unscaledTime + ResolveRetryInterval;

                try
                {
                    _bossPlayersType = FindType("friendlyPMC.Modules.BossPlayers");
                    _friendlyPmcType = FindType("friendlyPMC.friendlyPMC");
                    Type followerType = FindType("friendlyPMC.Components.BotFollowerPlayer");

                    _getFollowersMethod = _bossPlayersType?.GetMethod("GetFollowers", BindingFlags.Public | BindingFlags.Static);
                    _getBotMethod = followerType?.GetMethod("GetBot", BindingFlags.Public | BindingFlags.Instance);
                    _isSquadMateProperty = followerType?.GetProperty("IsSquadMate", BindingFlags.Public | BindingFlags.Instance);
                    _friendlyPmcFlagField = _friendlyPmcType?.GetField("friendlyPMCFLAG", BindingFlags.Public | BindingFlags.Static);
                    _badGuyField = _friendlyPmcType?.GetField("badGuy", BindingFlags.Public | BindingFlags.Static);

                    object configEntry = _friendlyPmcFlagField?.GetValue(null) ?? _badGuyField?.GetValue(null);
                    _configEntryValueProperty = configEntry?.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);

                    _available = _getFollowersMethod != null && _getBotMethod != null;
                }
                catch
                {
                    _available = false;
                }

                return _available;
            }

            private bool ReadConfigBool(FieldInfo field, bool fallback)
            {
                try
                {
                    object configEntry = field?.GetValue(null);
                    object rawValue = _configEntryValueProperty?.GetValue(configEntry, null);
                    return rawValue is bool boolValue ? boolValue : fallback;
                }
                catch
                {
                    return fallback;
                }
            }

            private static Type FindType(string fullName)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type type = assemblies[i].GetType(fullName, false);
                    if (type != null)
                        return type;
                }

                return null;
            }
        }

        private static string GetTrackingId(Player player)
        {
            if (!string.IsNullOrEmpty(player.ProfileId))
                return player.ProfileId;

            if (player.PlayerId != 0)
                return player.PlayerId.ToString();

            return player.GetInstanceID().ToString();
        }

        private void HideDots()
        {
            foreach (FriendlyNpcDot dot in _dots.Values)
                dot.SetVisible(false);
        }

        private void ClearDots()
        {
            foreach (FriendlyNpcDot dot in _dots.Values)
                dot.Destroy();

            _dots.Clear();
            _seenIds.Clear();
        }

        private void RemoveStaleDots()
        {
            List<string> staleIds = null;
            foreach (KeyValuePair<string, FriendlyNpcDot> entry in _dots)
            {
                if (_seenIds.Contains(entry.Key)) continue;

                if (staleIds == null)
                    staleIds = new List<string>();

                staleIds.Add(entry.Key);
            }

            if (staleIds == null) return;

            for (int i = 0; i < staleIds.Count; i++)
            {
                FriendlyNpcDot dot;
                if (_dots.TryGetValue(staleIds[i], out dot))
                    dot.Destroy();

                _dots.Remove(staleIds[i]);
            }
        }

        private void OnDestroy()
        {
            ClearDots();

            if (_instance == this)
                _instance = null;
        }

        private sealed class FriendlyNpcDot
        {
            private readonly Player _player;
            private readonly Renderer[] _renderers;
            private readonly ScreenSpaceDot _dot;

            public FriendlyNpcDot(Player player)
            {
                _player = player;
                _renderers = player != null ? player.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
                _dot = new ScreenSpaceDot("friendly_npc_dot");
            }

            public bool Update(Player localPlayer, Camera camera, int maxDistance)
            {
                if (!HasValidPlayer())
                {
                    SetVisible(false);
                    return false;
                }

                float distance = Vector3.Distance(localPlayer.Position, _player.Position);
                if (distance >= maxDistance)
                {
                    SetVisible(false);
                    return false;
                }

                Vector3 dotPosition = GetDotPosition();
                if (!HasLineOfSight(localPlayer, camera, dotPosition))
                {
                    SetVisible(false);
                    return false;
                }

                float alpha = 1f - Mathf.Clamp01(distance / maxDistance);
                _dot.Update(camera, dotPosition, GetDotSizePixels(), FriendlyDotColor, alpha);
                return true;
            }

            public void SetVisible(bool visible)
            {
                _dot.SetVisible(visible);
            }

            public void Destroy()
            {
                _dot.Destroy();
            }

            private bool HasValidPlayer()
            {
                return _player != null
                    && _player.gameObject.activeSelf
                    && _player.HealthController?.IsAlive == true;
            }

            private Vector3 GetDotPosition()
            {
                Bounds bounds;
                if (TryGetBounds(out bounds))
                    return new Vector3(bounds.center.x, bounds.max.y + DotTopOffset, bounds.center.z);

                return _player.Position + Vector3.up * FallbackDotHeight;
            }

            private static int GetDotSizePixels()
            {
                return Mathf.Clamp(BreakContactConfig.WorldMarkerDotSize.Value, 4, 64);
            }

            private bool HasLineOfSight(Player localPlayer, Camera camera, Vector3 dotPosition)
            {
                if (camera == null) return false;

                Vector3 rayStart = camera.transform.position;
                Bounds bounds;
                if (TryGetBounds(out bounds))
                {
                    Vector3 headPosition = new Vector3(
                        bounds.center.x,
                        Mathf.Max(bounds.center.y, bounds.max.y - 0.15f),
                        bounds.center.z);

                    if (HasClearRay(localPlayer, rayStart, headPosition))
                        return true;

                    if (HasClearRay(localPlayer, rayStart, bounds.center))
                        return true;
                }

                return HasClearRay(localPlayer, rayStart, dotPosition);
            }

            private bool HasClearRay(Player localPlayer, Vector3 rayStart, Vector3 rayEnd)
            {
                Vector3 ray = rayEnd - rayStart;
                float distance = ray.magnitude;
                if (distance <= 0.01f) return true;

                RaycastHit[] hits = Physics.RaycastAll(
                    rayStart,
                    ray / distance,
                    distance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore);

                if (hits == null || hits.Length == 0) return true;

                Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider hitCollider = hits[i].collider;
                    if (hitCollider == null) continue;
                    if (IsIgnoredLineOfSightCollider(localPlayer, hitCollider)) continue;

                    return false;
                }

                return true;
            }

            private bool IsIgnoredLineOfSightCollider(Player localPlayer, Collider hitCollider)
            {
                Transform hitTransform = hitCollider.transform;
                if (hitTransform == null) return false;

                if (_player != null && hitTransform.IsChildOf(_player.gameObject.transform))
                    return true;

                if (localPlayer != null && hitTransform.IsChildOf(localPlayer.gameObject.transform))
                    return true;

                return false;
            }

            private bool TryGetBounds(out Bounds bounds)
            {
                bool foundBounds = false;
                bounds = default(Bounds);

                for (int i = 0; i < _renderers.Length; i++)
                {
                    Renderer renderer = _renderers[i];
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;

                    if (!foundBounds)
                    {
                        bounds = renderer.bounds;
                        foundBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                return foundBounds;
            }
        }
    }
}
