﻿using Unity.Entities;
using VisualPinball.Engine.VPT.Plunger;
using VisualPinball.Unity.Game;

namespace VisualPinball.Unity.VPT.Plunger
{
	public class PlungerApi : ItemApi<Engine.VPT.Plunger.Plunger, PlungerData>
	{
		public bool DoRetract { get; set; } = true;

		public PlungerApi(Engine.VPT.Plunger.Plunger item, Entity entity, Player player) : base(item, entity, player)
		{
		}

		public void PullBack()
		{
			var movementData = EntityManager.GetComponentData<PlungerMovementData>(Entity);
			var velocityData = EntityManager.GetComponentData<PlungerVelocityData>(Entity);

			if (DoRetract) {
				PlungerCommands.PullBackAndRetract(Item.Data.SpeedPull, ref velocityData, ref movementData);

			} else {
				PlungerCommands.PullBack(Item.Data.SpeedPull, ref velocityData, ref movementData);
			}

			EntityManager.SetComponentData(Entity, movementData);
			EntityManager.SetComponentData(Entity, velocityData);
		}

		public void Fire()
		{
			var movementData = EntityManager.GetComponentData<PlungerMovementData>(Entity);
			var velocityData = EntityManager.GetComponentData<PlungerVelocityData>(Entity);
			var staticData = EntityManager.GetComponentData<PlungerStaticData>(Entity);

			// check for an auto plunger
			if (Item.Data.AutoPlunger) {
				// Auto Plunger - this models a "Launch Ball" button or a
				// ROM-controlled launcher, rather than a player-operated
				// spring plunger.  In a physical machine, this would be
				// implemented as a solenoid kicker, so the amount of force
				// is constant (modulo some mechanical randomness).  Simulate
				// this by triggering a release from the maximum retracted
				// position.
				PlungerCommands.Fire(1f, ref velocityData, ref movementData, in staticData);

			} else {
				// Regular plunger - trigger a release from the current
				// position, using the keyboard firing strength.

				var pos = (movementData.Position - staticData.FrameEnd) / (staticData.FrameStart - staticData.FrameEnd);
				PlungerCommands.Fire(pos, ref velocityData, ref movementData, in staticData);
			}

			EntityManager.SetComponentData(Entity, movementData);
			EntityManager.SetComponentData(Entity, velocityData);
		}
	}
}
