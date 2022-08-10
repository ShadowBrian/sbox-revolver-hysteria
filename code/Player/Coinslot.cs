using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace rh
{
	public partial class Coinslot : ModelEntity
	{
		[Net] public bool InsertedCoin { get; set; } = false;

		[Net] public int PlayerIndex { get; set; } = -1;

		[Net] public VRPlayer player { get; set; }

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/player/coinslot.vmdl" );
		}

		TimeSince TimeSinceSpawn;

		[Event.Tick.Server]
		public void Tick()
		{
			if ( InsertedCoin )
			{
				return;
			}

			if( TimeSinceSpawn > 1f && !player.IsValid() && PlayerIndex != -1 && PlayerIndex-1 < ( Game.Current as RevolverHysteriaGame).VRPlayers.Count)
			{
				player = (Game.Current as RevolverHysteriaGame).VRPlayers[PlayerIndex-1];
			}

			if ( player.IsValid() )
			{
				if ( player.LH != null && Vector3.DistanceBetween( player.LH.Position, GetAttachment( "slot" ).Value.Position ) < 5f )
				{
					PlaySound( "coininsert" );
					InsertedCoin = true;
					player.LH.PutCoin = true;
				}
			}
		}
	}
}
