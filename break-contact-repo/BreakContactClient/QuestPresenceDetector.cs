using BreakContact.Configs;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace BreakContact
{
    internal static class QuestPresenceDetector
    {
        private static ManualLogSource _logger;
        private const float ActiveQuestLocationRefreshInterval = 1f;
        private const float QuestLocationLootMarkerSuppressRadius = 8f;
        private static readonly HashSet<string> _trackedQuestMarkerIds = new HashSet<string>();
        private static readonly HashSet<string> _activeQuestLocationTargetIds = new HashSet<string>();
        private static readonly HashSet<QuestPresenceComponent> _activeQuestLootMarkers = new HashSet<QuestPresenceComponent>();
        private static readonly List<QuestPresenceComponent> _staleQuestLootMarkers = new List<QuestPresenceComponent>();
        private static readonly List<QuestLocationCandidate> _questLocationCandidates = new List<QuestLocationCandidate>();
        private static QuestPresenceWorldScanner _scanner;
        private static GameWorld _currentGameWorld;
        private static float _nextActiveQuestLocationRefreshTime;
        private static bool _questLocationCandidatesLoaded;

        public static void SetLogger(ManualLogSource logger)
        {
            _logger = logger;
        }

        public static void EnsureWorldScanner()
        {
            if (!BreakContactConfig.EnableQuestPresenceDetector.Value) return;
            EnsureCurrentGameWorld();
            if (_scanner != null) return;

            GameObject managerObject = new GameObject("BreakContactQuestPresenceScanner");
            UnityEngine.Object.DontDestroyOnLoad(managerObject);
            _scanner = managerObject.AddComponent<QuestPresenceWorldScanner>();
        }

        public static void ForgetWorldScanner(QuestPresenceWorldScanner scanner)
        {
            if (_scanner == scanner)
                _scanner = null;
        }

        public static void AttachToExistingQuestLootItems()
        {
            if (!BreakContactConfig.EnableQuestPresenceDetector.Value) return;
            EnsureCurrentGameWorld();

            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld?.LootList != null)
            {
                foreach (object lootObject in gameWorld.LootList)
                {
                    LootItem lootItem = lootObject as LootItem;
                    if (lootItem == null) continue;

                    AttachToLootItem(lootItem, lootItem?.Item);
                }
            }
        }

        public static void AttachToExistingQuestLocations(Player player)
        {
            if (!BreakContactConfig.EnableQuestPresenceDetector.Value) return;
            if (player == null || !player.IsYourPlayer) return;
            EnsureCurrentGameWorld();

            RefreshActiveQuestLocationTargets(player, true);
            if (_activeQuestLocationTargetIds.Count == 0) return;

            EnsureQuestLocationCandidates();
            for (int i = 0; i < _questLocationCandidates.Count; i++)
            {
                QuestLocationCandidate candidate = _questLocationCandidates[i];
                AttachToQuestLocation(candidate.Trigger, candidate.MarkerType);
            }
        }

        public static void AttachToLootItem(LootItem lootItem, Item item)
        {
            if (!BreakContactConfig.EnableQuestPresenceDetector.Value) return;
            if (Singleton<GameWorld>.Instance == null) return;
            EnsureCurrentGameWorld();
            if (lootItem == null || item == null || !IsQuestItem(item)) return;
            if (!lootItem.isActiveAndEnabled) return;

            string trackingId = !string.IsNullOrEmpty(item.Id)
                ? item.Id
                : (!string.IsNullOrEmpty(lootItem.ItemId) ? lootItem.ItemId : lootItem.GetInstanceID().ToString());
            trackingId = "loot:" + trackingId;
            if (!_trackedQuestMarkerIds.Add(trackingId)) return;

            GameObject tracker = new GameObject(lootItem.gameObject.name + "_qpd_tracker");
            QuestPresenceComponent component = tracker.AddComponent<QuestPresenceComponent>();
            component.SetItem(lootItem.gameObject, trackingId);
            tracker.transform.SetPositionAndRotation(lootItem.gameObject.transform.position, lootItem.gameObject.transform.rotation);
        }

        private static void AttachToQuestLocation(TriggerWithId trigger, string markerType)
        {
            if (trigger == null || !trigger.isActiveAndEnabled) return;

            string targetId = trigger.Id;
            if (string.IsNullOrEmpty(targetId)) return;
            if (!_activeQuestLocationTargetIds.Contains(targetId)) return;
            if (HasNearbyTrackedQuestLoot(trigger.transform.position)) return;

            string trackingId = markerType + ":" + targetId + ":" + trigger.GetInstanceID();
            if (!_trackedQuestMarkerIds.Add(trackingId)) return;

            GameObject tracker = new GameObject(trigger.gameObject.name + "_qpd_tracker");
            QuestPresenceComponent component = tracker.AddComponent<QuestPresenceComponent>();
            component.SetItem(trigger.gameObject, trackingId, targetId);
            tracker.transform.SetPositionAndRotation(trigger.gameObject.transform.position, trigger.gameObject.transform.rotation);
        }

        public static bool IsQuestItem(Item item)
        {
            if (item == null) return false;
            if (item.QuestItem) return true;

            bool questItem;
            if (TryReadQuestItemFlag(item, out questItem))
                return questItem;

            object template = GetMemberValue(item, "Template");
            if (TryReadQuestItemFlag(template, out questItem))
                return questItem;

            return false;
        }

        private static bool TryReadQuestItemFlag(object source, out bool value)
        {
            value = false;
            if (source == null) return false;

            if (TryReadBoolMember(source, "QuestItem", out value))
                return true;

            object props = GetMemberValue(source, "_props") ?? GetMemberValue(source, "Props") ?? GetMemberValue(source, "props");
            if (TryReadBoolMember(props, "QuestItem", out value))
                return true;

            return false;
        }

        private static bool TryReadBoolMember(object source, string memberName, out bool value)
        {
            value = false;

            object rawValue = GetMemberValue(source, memberName);
            if (rawValue is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            if (rawValue != null && bool.TryParse(rawValue.ToString(), out bool parsedValue))
            {
                value = parsedValue;
                return true;
            }

            return false;
        }

        public static void ForgetTrackedLootItem(string trackingId)
        {
            if (!string.IsNullOrEmpty(trackingId))
                _trackedQuestMarkerIds.Remove(trackingId);
        }

        public static void RegisterQuestLootMarker(QuestPresenceComponent marker)
        {
            if (marker != null)
                _activeQuestLootMarkers.Add(marker);
        }

        public static void ForgetQuestLootMarker(QuestPresenceComponent marker)
        {
            if (marker != null)
                _activeQuestLootMarkers.Remove(marker);
        }

        public static bool HasNearbyTrackedQuestLoot(Vector3 position)
        {
            return HasNearbyTrackedQuestLoot(position, QuestLocationLootMarkerSuppressRadius);
        }

        private static bool HasNearbyTrackedQuestLoot(Vector3 position, float maxDistance)
        {
            float maxDistanceSquared = maxDistance * maxDistance;
            bool foundNearbyMarker = false;
            _staleQuestLootMarkers.Clear();

            foreach (QuestPresenceComponent marker in _activeQuestLootMarkers)
            {
                if (marker == null || !marker.HasTrackedItem())
                {
                    _staleQuestLootMarkers.Add(marker);
                    continue;
                }

                if ((marker.GetMarkerAnchorPosition() - position).sqrMagnitude <= maxDistanceSquared)
                {
                    foundNearbyMarker = true;
                    break;
                }
            }

            for (int i = 0; i < _staleQuestLootMarkers.Count; i++)
                _activeQuestLootMarkers.Remove(_staleQuestLootMarkers[i]);

            return foundNearbyMarker;
        }

        public static bool IsActiveQuestLocationTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return false;

            Player player = Singleton<GameWorld>.Instance?.MainPlayer;
            if (player == null || !player.IsYourPlayer) return false;

            RefreshActiveQuestLocationTargets(player, false);
            return _activeQuestLocationTargetIds.Contains(targetId);
        }

        private static void RefreshActiveQuestLocationTargets(Player player, bool force)
        {
            if (!force && Time.time < _nextActiveQuestLocationRefreshTime) return;
            _nextActiveQuestLocationRefreshTime = Time.time + ActiveQuestLocationRefreshInterval;
            _activeQuestLocationTargetIds.Clear();

            try
            {
                if (player?.AbstractQuestControllerClass?.Quests == null) return;

                foreach (QuestClass quest in player.AbstractQuestControllerClass.Quests)
                {
                    if (quest == null || quest.QuestStatus != EQuestStatus.Started) continue;

                    foreach (Condition condition in quest.NecessaryConditions)
                    {
                        if (condition == null || quest.IsConditionDone(condition)) continue;
                        CollectQuestLocationTargetIds(condition, _activeQuestLocationTargetIds);
                    }
                }
            }
            catch (Exception ex)
            {
                LogWarning("Quest presence detector failed to refresh active quest location targets: " + ex.Message);
            }
        }

        private static void EnsureQuestLocationCandidates()
        {
            if (_questLocationCandidatesLoaded) return;

            _questLocationCandidatesLoaded = true;
            _questLocationCandidates.Clear();

            foreach (QuestTrigger trigger in UnityEngine.Object.FindObjectsOfType<QuestTrigger>())
                AddQuestLocationCandidate(trigger, "visit");

            foreach (PlaceItemTrigger trigger in UnityEngine.Object.FindObjectsOfType<PlaceItemTrigger>())
                AddQuestLocationCandidate(trigger, "place");
        }

        private static void AddQuestLocationCandidate(TriggerWithId trigger, string markerType)
        {
            if (trigger == null) return;
            _questLocationCandidates.Add(new QuestLocationCandidate(trigger, markerType));
        }

        private static void EnsureCurrentGameWorld()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (ReferenceEquals(gameWorld, _currentGameWorld)) return;

            _currentGameWorld = gameWorld;
            _trackedQuestMarkerIds.Clear();
            _activeQuestLocationTargetIds.Clear();
            _activeQuestLootMarkers.Clear();
            _staleQuestLootMarkers.Clear();
            _questLocationCandidates.Clear();
            _questLocationCandidatesLoaded = false;
            _nextActiveQuestLocationRefreshTime = 0f;
        }

        private static void CollectQuestLocationTargetIds(Condition condition, HashSet<string> targetIds)
        {
            if (condition == null || targetIds == null) return;

            if (condition is ConditionVisitPlace || condition is ConditionCompleteCondition)
            {
                AddCommonQuestLocationMembers(condition, targetIds);
                AddConditionId(condition, targetIds);
                return;
            }

            if (condition is ConditionLeaveItemAtLocation || condition is ConditionPlaceBeacon || condition is ConditionZone)
            {
                AddCommonQuestLocationMembers(condition, targetIds);
                AddConditionId(condition, targetIds);
                return;
            }

            if (condition is ConditionInZone)
            {
                AddCommonQuestLocationMembers(condition, targetIds);
                AddConditionId(condition, targetIds);
            }
        }

        private static void AddCommonQuestLocationMembers(Condition condition, HashSet<string> targetIds)
        {
            AddMemberString(condition, "target", targetIds);
            AddMemberString(condition, "targetId", targetIds);
            AddMemberString(condition, "zoneId", targetIds);
            AddMemberString(condition, "placeId", targetIds);
            AddMemberStringArray(condition, "targets", targetIds);
            AddMemberStringArray(condition, "targetIds", targetIds);
            AddMemberStringArray(condition, "zoneIds", targetIds);
        }

        private static void AddConditionId(Condition condition, HashSet<string> targetIds)
        {
            string conditionId = condition?.id.ToString();
            if (!string.IsNullOrEmpty(conditionId))
                targetIds.Add(conditionId);
        }

        private static void AddMemberString(object source, string memberName, HashSet<string> targetIds)
        {
            object value = GetMemberValue(source, memberName);
            if (value is string text && !string.IsNullOrEmpty(text))
                targetIds.Add(text);
        }

        private static void AddMemberStringArray(object source, string memberName, HashSet<string> targetIds)
        {
            object value = GetMemberValue(source, memberName);
            if (value is string[] texts)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    if (!string.IsNullOrEmpty(texts[i]))
                        targetIds.Add(texts[i]);
                }
            }
        }

        private static object GetMemberValue(object source, string memberName)
        {
            if (source == null || string.IsNullOrEmpty(memberName)) return null;

            Type type = source.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            while (type != null)
            {
                PropertyInfo property = type.GetProperty(memberName, flags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        return property.GetValue(source, null);
                    }
                    catch
                    {
                        return null;
                    }
                }

                FieldInfo field = type.GetField(memberName, flags);
                if (field != null)
                    return field.GetValue(source);

                type = type.BaseType;
            }

            return null;
        }

        public static void LogWarning(string message)
        {
            _logger?.LogWarning(message);
        }

        private sealed class QuestLocationCandidate
        {
            public readonly TriggerWithId Trigger;
            public readonly string MarkerType;

            public QuestLocationCandidate(TriggerWithId trigger, string markerType)
            {
                Trigger = trigger;
                MarkerType = markerType;
            }
        }
    }

    internal sealed class QuestPresenceWorldScanner : MonoBehaviour
    {
        private const float BootstrapScanInterval = 1f;
        private const int BootstrapScanAttempts = 2;
        private float _nextScanTime;
        private int _scanAttempts;

        private void Update()
        {
            if (!BreakContactConfig.EnableQuestPresenceDetector.Value)
            {
                Destroy(gameObject);
                return;
            }

            if (!BreakContactPlugin.InGame()) return;
            if (Time.time < _nextScanTime) return;

            _nextScanTime = Time.time + BootstrapScanInterval;

            Player player = Singleton<GameWorld>.Instance?.MainPlayer;
            if (player == null || !player.IsYourPlayer || player is HideoutPlayer) return;

            _scanAttempts++;
            QuestPresenceDetector.AttachToExistingQuestLootItems();
            QuestPresenceDetector.AttachToExistingQuestLocations(player);

            if (_scanAttempts >= BootstrapScanAttempts)
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            QuestPresenceDetector.ForgetWorldScanner(this);
        }
    }

    internal sealed class QuestPresenceComponent : MonoBehaviour
    {
        private const float DotTopOffset = 0.08f;
        private const float FallbackDotHeight = 0.35f;
        private const float QuestLocationDotHeight = 1.0f;
        private const float QuestLocationLootSuppressCheckInterval = 1f;
        private const float MaxLooseLootBoundsDimension = 3f;
        private const float MaxLooseLootAnchorDistance = 4f;

        private GameObject _item;
        private Transform _itemTransform;
        private Renderer[] _itemRenderers;
        private Collider[] _itemColliders;
        private string _trackingId;
        private string _questLocationTargetId;
        private ScreenSpaceDot _dot;
        private float _nextQuestLocationLootSuppressCheckTime;
        private int _cachedLooseBoundsFrame = -1;
        private bool _cachedLooseBoundsFound;
        private Bounds _cachedLooseBounds;

        public void SetItem(GameObject item, string trackingId, string questLocationTargetId = null)
        {
            _item = item;
            _itemTransform = item?.transform;
            _trackingId = trackingId;
            _questLocationTargetId = questLocationTargetId;
            _itemRenderers = item != null ? item.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            _itemColliders = item != null ? item.GetComponentsInChildren<Collider>(true) : new Collider[0];

            if (string.IsNullOrEmpty(_questLocationTargetId))
                QuestPresenceDetector.RegisterQuestLootMarker(this);
        }

        private void Update()
        {
            if (!BreakContactConfig.EnableQuestPresenceDetector.Value)
            {
                Destroy(gameObject);
                return;
            }

            if (!HasTrackedItem())
            {
                Destroy(gameObject);
                return;
            }

            if (!string.IsNullOrEmpty(_questLocationTargetId)
                && !QuestPresenceDetector.IsActiveQuestLocationTarget(_questLocationTargetId))
            {
                Destroy(gameObject);
                return;
            }

            if (!string.IsNullOrEmpty(_questLocationTargetId)
                && Time.time >= _nextQuestLocationLootSuppressCheckTime)
            {
                _nextQuestLocationLootSuppressCheckTime = Time.time + QuestLocationLootSuppressCheckInterval;
                if (QuestPresenceDetector.HasNearbyTrackedQuestLoot(GetMarkerAnchorPosition()))
                {
                    Destroy(gameObject);
                    return;
                }
            }

            Player player = Singleton<GameWorld>.Instance?.MainPlayer;
            Camera camera = ScreenSpaceDotResources.ResolvePlayerCamera();
            if (player == null || !player.IsYourPlayer || camera == null)
            {
                SetDotVisible(false);
                return;
            }

            int maxDistance = Mathf.Clamp(BreakContactConfig.QuestPresenceIndicatorMaxDistance.Value, 1, 100);
            Vector3 itemPosition = GetMarkerAnchorPosition();
            float playerDistance = Vector3.Distance(player.Position, itemPosition);
            if (playerDistance >= maxDistance)
            {
                SetDotVisible(false);
                return;
            }

            float alpha = 1f - Mathf.Clamp01(playerDistance / maxDistance);
            UpdateDot(camera, GetDotPosition(), alpha);
        }

        public bool HasTrackedItem()
        {
            return _item != null && _item.activeSelf;
        }

        public Vector3 GetMarkerAnchorPosition()
        {
            Bounds bounds;
            if (string.IsNullOrEmpty(_questLocationTargetId))
            {
                if (TryGetCachedLooseLootBounds(out bounds))
                    return bounds.center;
            }
            else if (TryGetColliderBounds(out bounds))
            {
                return bounds.center;
            }

            return _itemTransform != null ? _itemTransform.position : transform.position;
        }

        private Vector3 GetDotPosition()
        {
            Bounds bounds;
            if (!string.IsNullOrEmpty(_questLocationTargetId))
            {
                if (TryGetColliderBounds(out bounds))
                    return new Vector3(bounds.center.x, bounds.min.y + QuestLocationDotHeight, bounds.center.z);

                return _itemTransform.position + Vector3.up * QuestLocationDotHeight;
            }

            if (TryGetCachedLooseLootBounds(out bounds))
                return new Vector3(bounds.center.x, bounds.max.y + DotTopOffset, bounds.center.z);

            return _itemTransform.position + Vector3.up * FallbackDotHeight;
        }

        private bool TryGetCachedLooseLootBounds(out Bounds bounds)
        {
            if (_cachedLooseBoundsFrame == Time.frameCount)
            {
                bounds = _cachedLooseBounds;
                return _cachedLooseBoundsFound;
            }

            _cachedLooseBoundsFrame = Time.frameCount;
            _cachedLooseBoundsFound = TryGetLooseLootRendererBounds(out _cachedLooseBounds)
                || TryGetLooseLootColliderBounds(out _cachedLooseBounds);

            bounds = _cachedLooseBounds;
            return _cachedLooseBoundsFound;
        }

        private bool TryGetLooseLootRendererBounds(out Bounds bounds)
        {
            return TryGetLooseLootBounds(
                _itemRenderers,
                null,
                renderer => renderer != null ? renderer.bounds : default(Bounds),
                out bounds);
        }

        private bool TryGetLooseLootColliderBounds(out Bounds bounds)
        {
            if (TryGetLooseLootBounds(
                    _itemColliders,
                    collider => collider != null && !collider.isTrigger,
                    collider => collider != null ? collider.bounds : default(Bounds),
                    out bounds))
            {
                return true;
            }

            return TryGetLooseLootBounds(
                _itemColliders,
                null,
                collider => collider != null ? collider.bounds : default(Bounds),
                out bounds);
        }

        private bool TryGetLooseLootBounds<T>(T[] components, Func<T, bool> includeComponent, Func<T, Bounds> readBounds, out Bounds bounds) where T : Component
        {
            bounds = default(Bounds);
            if (components == null) return false;

            Bounds smallestBounds = default(Bounds);
            float smallestVolume = float.MaxValue;
            bool foundSmallest = false;
            bool foundAggregate = false;

            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (includeComponent != null && !includeComponent(component)) continue;
                if (!IsUsableBoundsComponent(component)) continue;

                Bounds candidate = readBounds(component);
                if (!IsValidBounds(candidate)) continue;

                float volume = GetBoundsVolume(candidate);
                if (volume < smallestVolume)
                {
                    smallestBounds = candidate;
                    smallestVolume = volume;
                    foundSmallest = true;
                }

                if (!IsReasonableLooseLootBounds(candidate)) continue;

                if (!foundAggregate)
                {
                    bounds = candidate;
                    foundAggregate = true;
                }
                else
                {
                    bounds.Encapsulate(candidate);
                }
            }

            if (foundAggregate) return true;
            if (!foundSmallest) return false;

            bounds = smallestBounds;
            return true;
        }

        private bool TryGetRendererBounds(out Bounds bounds)
        {
            bounds = default(Bounds);
            if (_itemRenderers == null) return false;

            bool foundBounds = false;
            for (int i = 0; i < _itemRenderers.Length; i++)
            {
                Renderer renderer = _itemRenderers[i];
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

        private bool TryGetColliderBounds(out Bounds bounds)
        {
            bounds = default(Bounds);
            if (_itemColliders == null) return false;

            bool foundBounds = false;
            for (int i = 0; i < _itemColliders.Length; i++)
            {
                Collider collider = _itemColliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;

                if (!foundBounds)
                {
                    bounds = collider.bounds;
                    foundBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return foundBounds;
        }

        private bool IsReasonableLooseLootBounds(Bounds bounds)
        {
            Vector3 size = bounds.size;
            if (size.x > MaxLooseLootBoundsDimension || size.y > MaxLooseLootBoundsDimension || size.z > MaxLooseLootBoundsDimension)
                return false;

            if (_itemTransform == null) return true;

            return (bounds.center - _itemTransform.position).sqrMagnitude <= MaxLooseLootAnchorDistance * MaxLooseLootAnchorDistance;
        }

        private static bool IsUsableBoundsComponent(Component component)
        {
            if (component == null || !component.gameObject.activeInHierarchy) return false;

            Renderer renderer = component as Renderer;
            if (renderer != null) return renderer.enabled;

            Collider collider = component as Collider;
            return collider == null || collider.enabled;
        }

        private static bool IsValidBounds(Bounds bounds)
        {
            Vector3 size = bounds.size;
            Vector3 center = bounds.center;
            return IsFinite(center.x) && IsFinite(center.y) && IsFinite(center.z)
                && IsFinite(size.x) && IsFinite(size.y) && IsFinite(size.z)
                && size.sqrMagnitude > 0.0001f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float GetBoundsVolume(Bounds bounds)
        {
            Vector3 size = bounds.size;
            return Mathf.Max(0.0001f, size.x * size.y * size.z);
        }

        private void UpdateDot(Camera camera, Vector3 position, float alpha)
        {
            EnsureDot();
            _dot?.Update(camera, position, GetDotSizePixels(), Color.white, alpha);
        }

        private static int GetDotSizePixels()
        {
            return Mathf.Clamp(BreakContactConfig.WorldMarkerDotSize.Value, 4, 64);
        }

        private void EnsureDot()
        {
            if (_dot == null)
                _dot = new ScreenSpaceDot("quest_objective_dot");
        }

        private void SetDotVisible(bool visible)
        {
            _dot?.SetVisible(visible);
        }

        private void OnDestroy()
        {
            _dot?.Destroy();

            QuestPresenceDetector.ForgetQuestLootMarker(this);
            QuestPresenceDetector.ForgetTrackedLootItem(_trackingId);
        }
    }

    internal sealed class ScreenSpaceDot
    {
        private readonly GameObject _gameObject;
        private readonly RectTransform _transform;
        private readonly RawImage _image;

        public ScreenSpaceDot(string name)
        {
            _gameObject = new GameObject(name, typeof(RectTransform));
            _gameObject.transform.SetParent(ScreenSpaceDotResources.Canvas.transform, false);

            _transform = _gameObject.GetComponent<RectTransform>();
            _transform.anchorMin = new Vector2(0.5f, 0.5f);
            _transform.anchorMax = new Vector2(0.5f, 0.5f);
            _transform.pivot = new Vector2(0.5f, 0.5f);

            _image = _gameObject.AddComponent<RawImage>();
            _image.texture = ScreenSpaceDotResources.Texture;
            _image.raycastTarget = false;
            _image.color = Color.white;

            _gameObject.SetActive(false);
        }

        public void Update(Camera camera, Vector3 worldPosition, float diameterPixels, Color color, float alpha)
        {
            if (camera == null)
            {
                SetVisible(false);
                return;
            }

            Vector3 viewportPosition = camera.WorldToViewportPoint(worldPosition);
            if (viewportPosition.z <= 0f)
            {
                SetVisible(false);
                return;
            }

            Vector2 screenPosition = new Vector2(
                viewportPosition.x * Screen.width,
                viewportPosition.y * Screen.height);

            RectTransform canvasTransform = ScreenSpaceDotResources.CanvasTransform;
            Vector2 anchoredPosition;
            if (canvasTransform == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasTransform,
                    screenPosition,
                    null,
                    out anchoredPosition))
            {
                SetVisible(false);
                return;
            }

            _gameObject.SetActive(true);
            _transform.anchoredPosition = anchoredPosition;
            _transform.sizeDelta = new Vector2(diameterPixels, diameterPixels);
            _image.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        public void SetVisible(bool visible)
        {
            if (_gameObject != null)
                _gameObject.SetActive(visible);
        }

        public void Destroy()
        {
            if (_gameObject != null)
                UnityEngine.Object.Destroy(_gameObject);
        }
    }

    internal static class ScreenSpaceDotResources
    {
        private const int TextureSize = 64;
        private static Canvas _canvas;
        private static RectTransform _canvasTransform;
        private static Texture2D _texture;

        public static Canvas Canvas
        {
            get
            {
                if (_canvas == null)
                    _canvas = CreateCanvas();

                return _canvas;
            }
        }

        public static RectTransform CanvasTransform
        {
            get
            {
                if (_canvasTransform == null)
                    _canvasTransform = Canvas.GetComponent<RectTransform>();

                return _canvasTransform;
            }
        }

        public static Texture2D Texture
        {
            get
            {
                if (_texture == null)
                    _texture = CreateTexture();

                return _texture;
            }
        }

        public static Camera ResolvePlayerCamera()
        {
            Camera mainCamera = Camera.main;
            if (IsUsableCamera(mainCamera))
                return mainCamera;

            Camera bestCamera = null;
            float bestScore = float.MinValue;
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (!IsUsableCamera(camera)) continue;

                float score = camera.depth;
                if (!camera.orthographic) score += 1000f;
                if (camera.targetTexture == null) score += 100f;
                if (camera.pixelWidth >= Screen.width * 0.75f && camera.pixelHeight >= Screen.height * 0.75f) score += 50f;

                string cameraName = camera.name.ToLowerInvariant();
                if (cameraName.Contains("fps")) score += 25f;
                if (cameraName.Contains("optic")) score -= 100f;
                if (cameraName.Contains("ui")) score -= 100f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCamera = camera;
                }
            }

            return bestCamera;
        }

        private static bool IsUsableCamera(Camera camera)
        {
            return camera != null
                && camera.enabled
                && camera.gameObject.activeInHierarchy
                && camera.pixelWidth > 0
                && camera.pixelHeight > 0;
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("BreakContactQuestPresenceOverlay");
            UnityEngine.Object.DontDestroyOnLoad(canvasObject);

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
                canvasObject.layer = uiLayer;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            canvas.pixelPerfect = false;
            _canvasTransform = canvasObject.GetComponent<RectTransform>();

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            return canvas;
        }

        private static Texture2D CreateTexture()
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.ARGB32, false);
            texture.name = "BreakContactQuestObjectiveDot";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float dx = ((x + 0.5f) / TextureSize) * 2f - 1f;
                    float dy = ((y + 0.5f) / TextureSize) * 2f - 1f;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = radius <= 0.72f ? 1f : Mathf.Clamp01((1f - radius) / 0.28f);
                    pixels[y * TextureSize + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }

    internal sealed class QuestPresenceLootItemPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LootItem).GetMethod(nameof(LootItem.Init));
        }

        [PatchPostfix]
        public static void PatchPostfix(LootItem __instance, Item item)
        {
            QuestPresenceDetector.AttachToLootItem(__instance, item);
        }
    }

    internal sealed class QuestPresencePlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod(nameof(Player.method_83));
        }

        [PatchPostfix]
        public static void PatchPostfix(Player __instance)
        {
            if (!BreakContactConfig.EnableQuestPresenceDetector.Value && !BreakContactConfig.EnableFriendlyNpcIndicator.Value) return;
            if (__instance == null || !__instance.IsYourPlayer || __instance is HideoutPlayer) return;
            if (Singleton<GameWorld>.Instance is HideoutGameWorld) return;

            if (BreakContactConfig.EnableQuestPresenceDetector.Value)
            {
                QuestPresenceDetector.EnsureWorldScanner();
                QuestPresenceDetector.AttachToExistingQuestLootItems();
                QuestPresenceDetector.AttachToExistingQuestLocations(__instance);
            }

            if (BreakContactConfig.EnableFriendlyNpcIndicator.Value)
                FriendlyNpcIndicator.Ensure();
        }
    }
}
