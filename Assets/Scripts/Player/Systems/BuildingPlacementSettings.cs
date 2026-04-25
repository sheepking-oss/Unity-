using UnityEngine;
using System.Collections.Generic;

namespace SurvivalGame.Player.Systems
{
    public enum PlacementValidationError
    {
        None = 0,
        NoGroundBelow = 1,
        SlopeTooSteep = 2,
        UnevenTerrain = 3,
        OverlappingWithObstacle = 4,
        TooCloseToOtherBuilding = 5,
        TooCloseToWall = 6,
        FloatingInAir = 7,
        PartialSupport = 8,
        WrongFoundationType = 9,
        PositionOutOfRange = 10,
        GroundHeightMismatch = 11,
        IntersectingWithPlayer = 12
    }

    [System.Serializable]
    public class PlacementValidationResult
    {
        public bool IsValid { get; private set; }
        public PlacementValidationError PrimaryError { get; private set; }
        public List<PlacementValidationError> AllErrors { get; private set; }
        public string ErrorMessage { get; private set; }
        public Vector3 SuggestedPosition { get; private set; }
        public float GroundHeight { get; private set; }
        public float SlopeAngle { get; private set; }
        public float HeightVariance { get; private set; }

        public PlacementValidationResult()
        {
            IsValid = true;
            PrimaryError = PlacementValidationError.None;
            AllErrors = new List<PlacementValidationError>();
            ErrorMessage = "Valid placement position";
            SuggestedPosition = Vector3.zero;
            GroundHeight = 0f;
            SlopeAngle = 0f;
            HeightVariance = 0f;
        }

        public void AddError(PlacementValidationError error, string message = "")
        {
            IsValid = false;

            if (!AllErrors.Contains(error))
            {
                AllErrors.Add(error);
            }

            if (PrimaryError == PlacementValidationError.None)
            {
                PrimaryError = error;
                if (!string.IsNullOrEmpty(message))
                {
                    ErrorMessage = message;
                }
                else
                {
                    ErrorMessage = GetErrorMessage(error);
                }
            }
        }

        public void SetGroundData(float groundHeight, float slopeAngle, float heightVariance, Vector3 suggestedPosition)
        {
            GroundHeight = groundHeight;
            SlopeAngle = slopeAngle;
            HeightVariance = heightVariance;
            SuggestedPosition = suggestedPosition;
        }

        private string GetErrorMessage(PlacementValidationError error)
        {
            return error switch
            {
                PlacementValidationError.NoGroundBelow => "No ground below this position",
                PlacementValidationError.SlopeTooSteep => "The ground is too steep",
                PlacementValidationError.UnevenTerrain => "The terrain is too uneven",
                PlacementValidationError.OverlappingWithObstacle => "Cannot place here - overlapping with obstacle",
                PlacementValidationError.TooCloseToOtherBuilding => "Too close to another building",
                PlacementValidationError.TooCloseToWall => "Cannot place inside or too close to a wall",
                PlacementValidationError.FloatingInAir => "Building would be floating in the air",
                PlacementValidationError.PartialSupport => "Building only has partial support",
                PlacementValidationError.WrongFoundationType => "Wrong type of foundation for this building",
                PlacementValidationError.PositionOutOfRange => "Position is too far away",
                PlacementValidationError.GroundHeightMismatch => "Ground height does not match",
                PlacementValidationError.IntersectingWithPlayer => "Cannot place inside player",
                _ => "Invalid placement position"
            };
        }
    }

    [System.Serializable]
    public class GroundSamplePoint
    {
        public Vector3 LocalPosition;
        public Vector3 WorldPosition;
        public float GroundHeight;
        public float SlopeAngle;
        public bool HitGround;
        public RaycastHit HitInfo;

        public GroundSamplePoint(Vector3 localPos)
        {
            LocalPosition = localPos;
            WorldPosition = Vector3.zero;
            GroundHeight = 0f;
            SlopeAngle = 0f;
            HitGround = false;
        }
    }

    [System.Serializable]
    public class BuildingPlacementSettings
    {
        [Header("General Settings")]
        [Tooltip("Maximum distance for placing buildings")]
        public float MaxPlacementDistance = 10f;

        [Header("Ground Validation")]
        [Tooltip("Maximum slope angle allowed for placement")]
        public float MaxSlopeAngle = 15f;

        [Tooltip("Maximum height variance across the building base")]
        public float MaxHeightVariance = 0.15f;

        [Tooltip("Height of the raycast for ground detection")]
        public float GroundDetectionHeight = 5f;

        [Tooltip("Extra depth to check below the building")]
        public float ExtraDepthCheck = 2f;

        [Header("Sample Points")]
        [Tooltip("Number of sample points along X axis (must be odd for center point)")]
        public int XSampleCount = 5;

        [Tooltip("Number of sample points along Z axis (must be odd for center point)")]
        public int ZSampleCount = 5;

        [Header("Collision Detection")]
        [Tooltip("Minimum distance to other buildings")]
        public float MinBuildingSpacing = 0.05f;

        [Tooltip("Minimum distance to walls/vertical obstacles")]
        public float MinWallDistance = 0.1f;

        [Tooltip("Collision margin around the building for detection")]
        public float CollisionMargin = 0.01f;

        [Tooltip("Layer mask for walls and vertical obstacles")]
        public LayerMask WallLayers;

        [Header("Foundation Settings")]
        [Tooltip("How much of the building must be supported")]
        [Range(0f, 1f)]
        public float RequiredSupportRatio = 1f;

        [Header("Grid Alignment")]
        [Tooltip("Snap to grid by default")]
        public bool SnapToGrid = true;

        [Tooltip("Also align to adjacent buildings")]
        public bool AlignToAdjacentBuildings = true;

        [Tooltip("Search radius for adjacent buildings to align with")]
        public float AdjacentBuildingSearchRadius = 3f;

        [Header("Debug")]
        public bool ShowDebugGizmos = true;
        public bool LogValidationErrors = false;
    }
}
