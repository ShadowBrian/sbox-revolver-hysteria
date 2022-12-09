using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Editor;

namespace rh
{
	[HammerEntity]
	[Library( "ent_rh_shootable_health" )]
	[SupportsSolid]
	[Model]
	public partial class HealthItem : ModelEntity
	{
		public override void Spawn()
		{
			base.Spawn();
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		}

		public override void TakeDamage( DamageInfo info )
		{
			base.TakeDamage( info );

			if ( info.Attacker is VRPlayer player )
			{
				if ( player.HeadEnt.HitPoints < 5 )
				{
					player.HeadEnt.AddHealth();

					Delete();
				}
			}
		}
	}
}
