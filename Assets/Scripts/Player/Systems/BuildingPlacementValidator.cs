using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Data.Buildings;
using SurvivalGame.World.Buildings;

namespace SurvivalGame.Player.Systems
{
    public static class BuildingPlacementValidator
    {
        public static PlacementValidationResult ValidatePlacement(
            BuildingData buildingData,
            Vector3 position,
            Quaternion rotation,
            BuildingPlacementSettings settings,
            LayerMask groundLayers,
            LayerMask obstacleLayers,
            Transform playerTransform = null,
            IEnumerable<Building> existingBuildings = null)
        {
            PlacementValidationResult result = new PlacementValidationResult();

            if (buildingData == null)
            {
                result.AddError(PlacementValidationError.PositionOutOfRange, "No building data");
                return result;
            }

            BuildingPlacementSettings mergedSettings = MergeSettings(buildingData, settings);

            Vector3 buildingSize = buildingData.Size;
            Vector3 halfSize = buildingSize * 0.5f;
            Vector3 placementOffset = buildingData.PlacementOffset;
            Vector3 centerPosition = position + placementOffset;

            List<GroundSamplePoint> samplePoints = GenerateSamplePoints(centerPosition, halfSize, mergedSettings);
            SampleGroundPoints(samplePoints, centerPosition, mergedSettings, groundLayers);

            AnalyzeGroundPoints(samplePoints, result, mergedSettings, buildingData);

            if (!result.IsValid)
            {
                return result;
            }

            CheckCollision(buildingData, centerPosition, rotation, result, mergedSettings, obstacleLayers, playerTransform);

            if (!result.IsValid)
            {
                return result;
            }

            CheckWallProximity(buildingData, centerPosition, rotation, result, mergedSettings);

            if (!result.IsValid)
            {
                return result;
            }

            if (buildingData.RequiresFoundation)
            {
                CheckFoundationSupport(buildingData, centerPosition, result, existingBuildings);
            }
            else
            {
                CheckGroundSupport(samplePoints, result, mergedSettings, buildingData);
            }

            if (!result.IsValid)
            {
                return result;
            }

            CheckBuildingSpacing(buildingData, centerPosition, rotation, result, mergedSettings, existingBuildings);

            return result;
        }

        public static BuildingPlacementSettings MergeSettings(BuildingData buildingData, BuildingPlacementSettings baseSettings)
        {
            BuildingPlacementSettings merged = new BuildingPlacementSettings();

            merged.MaxPlacementDistance = baseSettings.MaxPlacementDistance;

            if (buildingData.UseCustomSlopeLimit)
            {
                merged.MaxSlopeAngle = buildingData.MaxAllowedSlopeAngle;
            }
            else
            {
                merged.MaxSlopeAngle = baseSettings.MaxSlopeAngle;
            }

            if (buildingData.UseCustomHeightVariance)
            {
                merged.MaxHeightVariance = buildingData.MaxAllowedHeightVariance;
            }
            else
            {
                merged.MaxHeightVariance = baseSettings.MaxHeightVariance;
            }

            merged.GroundDetectionHeight = buildingData.GroundDetectionHeight > 0
                ? buildingData.GroundDetectionHeight
                : baseSettings.GroundDetectionHeight;

            merged.ExtraDepthCheck = buildingData.ExtraDepthCheck > 0
                ? buildingData.ExtraDepthCheck
                : baseSettings.ExtraDepthCheck;

            merged.XSampleCount = buildingData.GroundSampleCountX > 0
                ? buildingData.GroundSampleCountX
                : baseSettings.XSampleCount;

            merged.ZSampleCount = buildingData.GroundSampleCountZ > 0
                ? buildingData.GroundSampleCountZ
                : baseSettings.ZSampleCount;

            if (buildingData.UseCustomSpacing)
            {
                merged.MinBuildingSpacing = buildingData.MinimumSpacingToOtherBuildings;
                merged.MinWallDistance = buildingData.MinimumSpacingToWalls;
            }
            else
            {
                merged.MinBuildingSpacing = baseSettings.MinBuildingSpacing;
                merged.MinWallDistance = baseSettings.MinWallDistance;
            }

            merged.CollisionMargin = buildingData.CollisionMargin >= 0
                ? buildingData.CollisionMargin
                : baseSettings.CollisionMargin;

            merged.WallLayers = baseSettings.WallLayers;
            merged.RequiredSupportRatio = buildingData.RequiredSupportRatio;

            merged.SnapToGrid = buildingData.SnapToGrid;
            merged.AlignToAdjacentBuildings = buildingData.AlignToAdjacentBuildings;
            merged.AdjacentBuildingSearchRadius = baseSettings.AdjacentBuildingSearchRadius;

            merged.ShowDebugGizmos = baseSettings.ShowDebugGizmos;
            merged.LogValidationErrors = baseSettings.LogValidationErrors;

            return merged;
        }

        public static void ApplyValidationRules(PlacementValidationRule rule, BuildingPlacementSettings settings)
        {
            switch (rule)
            {
                case PlacementValidationRule.Default:
                    break;
                case PlacementValidationRule.Strict:
                    settings.MaxSlopeAngle = 5f;
                    settings.MaxHeightVariance = 0.05f;
                    settings.MinBuildingSpacing = 0.1f;
                    settings.MinWallDistance = 0.15f;
                    settings.RequiredSupportRatio = 1f;
                    settings.CollisionMargin = 0.05f;
                    break;
                case PlacementValidationRule.Relaxed:
                    settings.MaxSlopeAngle = 30f;
                    settings.MaxHeightVariance = 0.3f;
                    settings.MinBuildingSpacing = 0.01f;
                    settings.MinWallDistance = 0.01f;
                    settings.RequiredSupportRatio = 0.5f;
                    settings.CollisionMargin = 0f;
                    break;
                case PlacementValidationRule.IgnoreAll:
                    settings.MaxSlopeAngle = 90f;
                    settings.MaxHeightVariance = 100f;
                    settings.MinBuildingSpacing = -100f;
                    settings.MinWallDistance = -100f;
                    settings.RequiredSupportRatio = 0f;
                    settings.CollisionMargin = -1f;
                    break;
            }
        }

        public static List<GroundSamplePoint> GenerateSamplePoints(
            Vector3 centerPosition,
            Vector3 halfSize,
            BuildingPlacementSettings settings)
        {
            List<GroundSamplePoint> points = new List<GroundSamplePoint>();

            int xSamples = Mathf.Max(1, settings.XSampleCount);
            int zSamples = Mathf.Max(1, settings.ZSampleCount);

            float halfX = halfSize.x;
            float halfZ = halfSize.z;

            float xStep = (halfX * 2) / Mathf.Max(1, xSamples - 1);
            float zStep = (halfZ * 2) / Mathf.Max(1, zSamples - 1);

            for (int i = 0; i < xSamples; i++)
            {
                for (int j = 0; j < zSamples; j++)
                {
                    float localX = -halfX + (i * xStep);
                    float localZ = -halfZ + (j * zStep);

                    Vector3 localPos = new Vector3(localX, 0f, localZ);
                    points.Add(new GroundSamplePoint(localPos));
                }
            }

            return points;
        }

        public static void SampleGroundPoints(
            List<GroundSamplePoint> points,
            Vector3 centerPosition,
            BuildingPlacementSettings settings,
            LayerMask groundLayers)
        {
            float sampleHeight = settings.GroundDetectionHeight;
            float maxDistance = sampleHeight + settings.ExtraDepthCheck;

            foreach (GroundSamplePoint point in points)
            {
                Vector3 worldPos = centerPosition + point.LocalPosition;
                Vector3 rayStart = worldPos + Vector3.up * sampleHeight;
                Vector3 rayEnd = worldPos - Vector3.up * settings.ExtraDepthCheck;

                point.WorldPosition = worldPos;

                if (Physics.Linecast(rayStart, rayEnd, out RaycastHit hit, groundLayers))
                {
                    point.HitGround = true;
                    point.GroundHeight = hit.point.y;
                    point.SlopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                    point.HitInfo = hit;
                }
                else
                {
                    point.HitGround = false;
                    point.GroundHeight = float.MinValue;
                    point.SlopeAngle = 90f;
                }
            }
        }

        public static void AnalyzeGroundPoints(
            List<GroundSamplePoint> points,
            PlacementValidationResult result,
            BuildingPlacementSettings settings,
            BuildingData buildingData = null)
        {
            if (points.Count == 0)
            {
                result.AddError(PlacementValidationError.NoGroundBelow);
                return;
            }

            float totalGroundHeight = 0f;
            int groundHitCount = 0;
            float maxSlope = 0f;
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            Vector3 suggestedPosition = Vector3.zero;
            bool hasCenterSample = false;
            float centerHeight = 0f;

            foreach (GroundSamplePoint point in points)
            {
                if (point.LocalPosition == Vector3.zero)
                {
                    hasCenterSample = true;
                    if (point.HitGround)
                    {
                        centerHeight = point.GroundHeight;
                    }
                }

                if (point.HitGround)
                {
                    groundHitCount++;
                    totalGroundHeight += point.GroundHeight;
                    maxSlope = Mathf.Max(maxSlope, point.SlopeAngle);
                    minHeight = Mathf.Min(minHeight, point.GroundHeight);
                    maxHeight = Mathf.Max(maxHeight, point.GroundHeight);
                }
            }

            float heightVariance = maxHeight - minHeight;

            float avgGroundHeight = 0f;
            if (groundHitCount > 0)
            {
                avgGroundHeight = totalGroundHeight / groundHitCount;

                float finalY = hasCenterSample && buildingData != null && buildingData.AutoSnapToGridLine
                    ? centerHeight
                    : avgGroundHeight;

                suggestedPosition = new Vector3(
                    points[0].WorldPosition.x - points[0].LocalPosition.x,
                    finalY,
                    points[0].WorldPosition.z - points[0].LocalPosition.z
                );
            }

            result.SetGroundData(avgGroundHeight, maxSlope, heightVariance, suggestedPosition);

            if (groundHitCount == 0)
            {
                result.AddError(PlacementValidationError.FloatingInAir);
                return;
            }

            bool allowOnSlope = buildingData != null && buildingData.AllowOnSlope;
            if (!allowOnSlope && maxSlope > settings.MaxSlopeAngle)
            {
                result.AddError(PlacementValidationError.SlopeTooSteep,
                    $"Slope too steep ({maxSlope:F1}° > {settings.MaxSlopeAngle:F1}°)");
            }

            if (heightVariance > settings.MaxHeightVariance)
            {
                result.AddError(PlacementValidationError.UnevenTerrain,
                    $"Terrain too uneven ({heightVariance:F2}m > {settings.MaxHeightVariance:F2}m)");
            }
        }

        public static void CheckCollision(
            BuildingData buildingData,
            Vector3 centerPosition,
            Quaternion rotation,
            PlacementValidationResult result,
            BuildingPlacementSettings settings,
            LayerMask obstacleLayers,
            Transform playerTransform)
        {
            Vector3 halfSize = buildingData.Size * 0.5f;
            float margin = settings.CollisionMargin;
            Vector3 marginExtents = halfSize + Vector3.one * margin;

            Collider[] hitColliders = Physics.OverlapBox(
                centerPosition,
                marginExtents,
                rotation,
                obstacleLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider hit in hitColliders)
            {
                if (hit == null) continue;

                if (playerTransform != null)
                {
                    if (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform))
                    {
                        result.AddError(PlacementValidationError.IntersectingWithPlayer);
                        return;
                    }
                }

                Building building = hit.GetComponentInParent<Building>();
                if (building != null && building.BuildingData != null)
                {
                    if (building.BuildingData.Category == BuildingCategory.Foundation)
                    {
                        continue;
                    }
                }

                result.AddError(PlacementValidationError.OverlappingWithObstacle,
                    $"Overlapping with: {hit.gameObject.name}");
                return;
            }
        }

        public static void CheckWallProximity(
            BuildingData buildingData,
            Vector3 centerPosition,
            Quaternion rotation,
            PlacementValidationResult result,
            BuildingPlacementSettings settings)
        {
            if (settings.WallLayers == 0) return;

            Vector3 halfSize = buildingData.Size * 0.5f;
            float margin = settings.MinWallDistance;
            Vector3 wallCheckExtents = halfSize + Vector3.one * margin;

            Collider[] wallColliders = Physics.OverlapBox(
                centerPosition,
                wallCheckExtents,
                rotation,
                settings.WallLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider wall in wallColliders)
            {
                if (wall == null) continue;

                Building building = wall.GetComponentInParent<Building>();
                if (building != null)
                {
                    if (building.BuildingData != null &&
                        (building.BuildingData.Category == BuildingCategory.Foundation ||
                         building.BuildingData.Category == BuildingCategory.Wall ||
                         building.BuildingData.Category == BuildingCategory.Floor))
                    {
                        continue;
                    }
                }

                result.AddError(PlacementValidationError.TooCloseToWall);
                return;
            }
        }

        public static void CheckGroundSupport(
            List<GroundSamplePoint> points,
            PlacementValidationResult result,
            BuildingPlacementSettings settings,
            BuildingData buildingData = null)
        {
            int totalPoints = points.Count;
            int supportedPoints = 0;

            foreach (GroundSamplePoint point in points)
            {
                if (point.HitGround)
                {
                    supportedPoints++;
                }
            }

            float supportRatio = (float)supportedPoints / totalPoints;
            bool allowPartial = buildingData != null && buildingData.AllowPartialSupport;

            if (!allowPartial && supportRatio < settings.RequiredSupportRatio)
            {
                result.AddError(PlacementValidationError.PartialSupport,
                    $"Insufficient support ({supportRatio:P0} < {settings.RequiredSupportRatio:P0})");
            }
        }

        public static void CheckFoundationSupport(
            BuildingData buildingData,
            Vector3 centerPosition,
            PlacementValidationResult result,
            IEnumerable<Building> existingBuildings)
        {
            Vector3 checkCenter = centerPosition - Vector3.up * 0.2f;
            Vector3 checkExtents = new Vector3(
                buildingData.Size.x * 0.4f,
                0.2f,
                buildingData.Size.z * 0.4f
            );

            bool foundFoundation = false;

            Collider[] colliders = Physics.OverlapBox(checkCenter, checkExtents);
            foreach (Collider col in colliders)
            {
                Building building = col.GetComponentInParent<Building>();
                if (building != null && building.BuildingData != null)
                {
                    if (building.BuildingData.Category == BuildingCategory.Foundation)
                    {
                        foundFoundation = true;
                        break;
                    }
                }
            }

            if (existingBuildings != null)
            {
                foreach (Building building in existingBuildings)
                {
                    if (building == null || building.BuildingData == null) continue;
                    if (building.BuildingData.Category != BuildingCategory.Foundation) continue;

                    float distance = Vector3.Distance(centerPosition, building.transform.position);
                    if (distance < 5f)
                    {
                        foundFoundation = true;
                        break;
                    }
                }
            }

            if (!foundFoundation)
            {
                result.AddError(PlacementValidationError.WrongFoundationType, "Requires foundation");
            }
        }

        public static void CheckBuildingSpacing(
            BuildingData buildingData,
            Vector3 centerPosition,
            Quaternion rotation,
            PlacementValidationResult result,
            BuildingPlacementSettings settings,
            IEnumerable<Building> existingBuildings)
        {
            if (existingBuildings == null) return;

            Vector3 halfSize = buildingData.Size * 0.5f;
            float minSpacing = settings.MinBuildingSpacing;

            foreach (Building building in existingBuildings)
            {
                if (building == null || building.BuildingData == null) continue;

                if (building.BuildingData.Category == BuildingCategory.Foundation)
                {
                    continue;
                }

                Vector3 otherCenter = building.transform.position + building.BuildingData.PlacementOffset;
                Vector3 otherHalfSize = building.BuildingData.Size * 0.5f;

                float xDistance = Mathf.Abs(centerPosition.x - otherCenter.x) - (halfSize.x + otherHalfSize.x);
                float zDistance = Mathf.Abs(centerPosition.z - otherCenter.z) - (halfSize.z + otherHalfSize.z);

                if (xDistance < minSpacing && zDistance < minSpacing)
                {
                    result.AddError(PlacementValidationError.TooCloseToOtherBuilding,
                        $"Too close to {building.BuildingData.BuildingName}");
                    return;
                }
            }
        }

        public static Vector3 AlignToGrid(Vector3 position, float gridSize)
        {
            return new Vector3(
                Mathf.Round(position.x / gridSize) * gridSize,
                position.y,
                Mathf.Round(position.z / gridSize) * gridSize
            );
        }

        public static Vector3 AlignToAdjacentBuildings(
            Vector3 position,
            float gridSize,
            float searchRadius,
            IEnumerable<Building> existingBuildings)
        {
            if (existingBuildings == null) return position;

            Vector3 bestAlignment = position;
            float bestDistance = float.MaxValue;

            foreach (Building building in existingBuildings)
            {
                if (building == null || building.BuildingData == null) continue;

                Vector3 buildingPos = building.transform.position + building.BuildingData.PlacementOffset;
                float distance = Vector3.Distance(position, buildingPos);

                if (distance <= searchRadius && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestAlignment = new Vector3(
                        buildingPos.x,
                        position.y,
                        buildingPos.z
                    );
                }
            }

            if (bestDistance != float.MaxValue)
            {
                return AlignToGrid(bestAlignment, gridSize);
            }

            return position;
        }
    }
}
