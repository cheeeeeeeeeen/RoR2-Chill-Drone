using Chen.GradiusMod.Drones;
using RoR2;

namespace Chen.ChillDrone.Drone.States
{
    internal class DeathState : DroneDeathState
    {
        protected override bool SpawnInteractable { get; set; } = ChillDrone.instance.canBeRepurchased;

        protected override InteractableSpawnCard GetInteractableSpawnCard => ChillDrone.instance.iSpawnCard;
    }
}