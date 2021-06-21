using Chen.GradiusMod.Drones;
using RoR2;
using System;

namespace Chen.ChillDrone.Drone.States
{
    internal class DeathState : DroneDeathState
    {
        protected override bool SpawnInteractable { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        protected override InteractableSpawnCard GetInteractableSpawnCard => throw new NotImplementedException();
    }
}