using System;
using Datatypes;
using Environments;
using UnityEngine;

namespace Behaviour
{
    public class Animal : LivingEntity
    {
        public enum CreatureAction
        {
            None,
            Resting,
            Exploring,
            GoingToFood,
            GoingToWater,
            Eating,
            Drinking,
            Decaying
        }

        public enum Diet
        {
            Herbivore,
            Carnivore
        }

        public const int maxViewDistance = 10;
        private const float sqrtTwo = 1.4142f;
        private const float oneOverSqrtTwo = 1 / sqrtTwo;

        // Move data:
        private bool animatingMovement;

        private readonly float criticalPercent = 0.7f;
        public CreatureAction currentAction;

        [Header("Settings")]
        public Diet diet;

        private readonly float drinkDuration = 6;
        private readonly float eatDuration = 10;

        protected LivingEntity foodTarget;

        // State:
        [Header("State")]
        public float hunger;

        // Other
        private float lastActionChooseTime;

        // Visual settings:
        private readonly float moveArcHeight = .2f;
        private float moveArcHeightFactor;
        private Coord moveFromCoord;
        private readonly float moveSpeed = 1.5f;
        private float moveSpeedFactor;
        private Vector3 moveStartPos;
        private Coord moveTargetCoord;
        private Vector3 moveTargetPos;
        private float moveTime;
        private Coord[] path;
        private int pathIndex;
        public float thirst;

        // Settings:
        private readonly float timeBetweenActionChoices = 1;
        private readonly float timeToDeathByHunger = 120;
        private readonly float timeToDeathByThirst = 200;
        protected Coord waterTarget;

        public override void Init(Coord startingCoord)
        {
            base.Init(startingCoord);
            moveFromCoord = startingCoord;

            StatsTracker.AddEntity(id);

            ChooseNextAction();
        }

        public override void Update()
        {
            base.Update();

            if (dead)
            {
                currentAction = CreatureAction.Decaying;

                //Do decaying extra stuff here
                return;
            }

            // Increase hunger and thirst over time
            hunger += Time.deltaTime * 1 / timeToDeathByHunger;
            thirst += Time.deltaTime * 1 / timeToDeathByThirst;

            if (hunger >= timeToDeathByHunger || thirst >= timeToDeathByThirst)
            {
                Die(hunger >= timeToDeathByHunger ? "Hunger" : "Thirst");
            }

            // Animate movement. After moving a single tile, the animal will be able to choose its next action
            if (animatingMovement)
            {
                AnimateMove();
            }
            else
            {
                // Handle interactions with external things, like food, water, mates
                HandleInteractions();
                var timeSinceLastActionChoice = Time.time - lastActionChooseTime;
                if (timeSinceLastActionChoice > timeBetweenActionChoices)
                {
                    ChooseNextAction();
                }
            }
        }

        // Animals choose their next action after each movement step (1 tile),
        // or when not moving (e.g interacting with food etc), at fixed time intervals
        protected virtual void ChooseNextAction()
        {
            lastActionChooseTime = Time.time;
            // Get info about surroundings
            var surroundings = Environments.Environment.Sense(coord);

            // Decide next action:
            // Eat if (more hungry than thirsty) or (currently eating and not critically thirsty)
            var currentlyEating = currentAction == CreatureAction.Eating && foodTarget && hunger > 0;
            if (hunger >= thirst || currentlyEating && thirst < criticalPercent)
            {
                if (surroundings.nearestFoodSource)
                {
                    currentAction = CreatureAction.GoingToFood;
                    foodTarget = surroundings.nearestFoodSource;
                    CreatePath(foodTarget.coord);
                }
                else
                {
                    currentAction = CreatureAction.Exploring;
                }
            }
            // More thirsty than hungry
            else
            {
                if (surroundings.nearestWaterTile != Coord.Invalid)
                {
                    currentAction = CreatureAction.GoingToWater;
                    waterTarget = surroundings.nearestWaterTile;
                    CreatePath(waterTarget);
                }
                else
                {
                    currentAction = CreatureAction.Exploring;
                }
            }

            Act();
        }

        protected void CreatePath(Coord target)
        {
            // Create new path if current is not already going to target
            if (path == null || pathIndex >= path.Length || path[path.Length - 1] != target || path[pathIndex] != coord)
            {
                path = EnvironmentUtility.GetPath(coord.x, coord.y, target.x, target.y);
                pathIndex = 0;
            }
        }

        protected void Act()
        {
            switch (currentAction)
            {
                case CreatureAction.None:
                    break;
                case CreatureAction.Exploring:
                    StartMoveToCoord(Environments.Environment.GetNextTileWeighted(coord, moveFromCoord));
                    break;
                case CreatureAction.GoingToFood:
                    if (Coord.AreNeighbours(coord, foodTarget.coord))
                    {
                        LookAt(foodTarget.coord);
                        currentAction = CreatureAction.Eating;
                    }
                    else
                    {
                        //StartMoveToCoord (EnvironmentUtility.GetNextInPath (coord.x, coord.y, foodTarget.coord.x, foodTarget.coord.y), true);
                        StartMoveToCoord(path[pathIndex], true);
                        pathIndex++;
                    }

                    break;
                case CreatureAction.GoingToWater:
                    if (Coord.AreNeighbours(coord, waterTarget))
                    {
                        LookAt(waterTarget);
                        currentAction = CreatureAction.Drinking;
                    }
                    else
                    {
                        StartMoveToCoord(path[pathIndex], true);
                        pathIndex++;
                    }
                    break;
                case CreatureAction.Resting:
                    break;
                case CreatureAction.Eating:
                    break;
                case CreatureAction.Drinking:
                    break;
                default:
                    Debug.LogException(new ArgumentOutOfRangeException());
                    break;
            }
        }

        protected void StartMoveToCoord(Coord target, bool followingPath = false)
        {
            moveFromCoord = coord;
            moveTargetCoord = target;
            moveStartPos = transform.position;
            moveTargetPos = Environments.Environment.tileCentres[moveTargetCoord.x, moveTargetCoord.y];
            animatingMovement = true;

            var diagonalMove = Coord.SqrDistance(moveFromCoord, moveTargetCoord) > 1;
            moveArcHeightFactor = diagonalMove ? sqrtTwo : 1;
            moveSpeedFactor = diagonalMove ? oneOverSqrtTwo : 1;

            LookAt(moveTargetCoord);
        }

        protected void LookAt(Coord target)
        {
            if (target == coord)
            {
                return;
            }

            var offset = target - coord;
            transform.eulerAngles = Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg * Vector3.up;
        }

        private void HandleInteractions()
        {
            if (currentAction == CreatureAction.Eating)
            {
                if (foodTarget && hunger > 0)
                {
                    var eatAmount = Mathf.Min(hunger, Time.deltaTime * 1 / eatDuration);
                    eatAmount = ((Plant) foodTarget).Consume(eatAmount);
                    hunger -= eatAmount;
                }
            }
            else if (currentAction == CreatureAction.Drinking)
            {
                if (thirst > 0)
                {
                    thirst -= Time.deltaTime * 1 / drinkDuration;
                    thirst = Mathf.Clamp01(thirst);
                }
            }
        }

        private void AnimateMove()
        {
            // Move in an arc from start to end tile
            moveTime = Mathf.Min(1, moveTime + Time.deltaTime * moveSpeed * moveSpeedFactor);
            var height = (1 - 4 * (moveTime - .5f) * (moveTime - .5f)) * moveArcHeight * moveArcHeightFactor;
            transform.position = Vector3.Lerp(moveStartPos, moveTargetPos, moveTime) + Vector3.up * height;

            // Finished moving
            if (moveTime >= 1)
            {
                Environments.Environment.RegisterMove(this, coord, moveTargetCoord);
                coord = moveTargetCoord;

                animatingMovement = false;
                moveTime = 0;
                ChooseNextAction();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying)
            {
                var surroundings = Environments.Environment.Sense(coord);
                Gizmos.color = Color.white;
                if (surroundings.nearestFoodSource != null)
                {
                    Gizmos.DrawLine(transform.position, surroundings.nearestFoodSource.transform.position);
                }
                if (surroundings.nearestWaterTile != Coord.Invalid)
                {
                    Gizmos.DrawLine(
                        transform.position,
                        Environments.Environment.tileCentres[surroundings.nearestWaterTile.x,
                        surroundings.nearestWaterTile.y]
                        );
                }

                if (currentAction == CreatureAction.GoingToFood)
                {
                    var next = EnvironmentUtility.GetNextInPath(coord.x, coord.y, foodTarget.coord.x,
                        foodTarget.coord.y);
                    var path = EnvironmentUtility.GetPath(coord.x, coord.y, foodTarget.coord.x, foodTarget.coord.y);
                    Gizmos.color = Color.black;
                    for (var i = 0; i < path.Length; i++)
                        Gizmos.DrawSphere(Environments.Environment.tileCentres[path[i].x, path[i].y], .2f);
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(Environments.Environment.tileCentres[next.x, next.y] + Vector3.up * .1f, .25f);
                }
            }
        }
    }
}