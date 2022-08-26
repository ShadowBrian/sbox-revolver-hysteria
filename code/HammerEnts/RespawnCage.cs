using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using SandboxEditor;

namespace rh
{
	[HammerEntity]
	[Library( "ent_rh_respawncage" )]
	[Model( Model = "models/player/respawncage.vmdl" )]
	public partial class RespawnCage : AnimatedEntity
	{
		[Property]
		[Net] public int HitsRequired { get; set; } = 1;
		[Net, Predicted] public VRPlayer OccupyingPlayer { get; set; }

		[Net, Predicted] public bool UsedCage { get; set; }

		public Output OnPlayerSaved { get; set; }

		public override void Spawn()
		{
			base.Spawn();
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		}

		[Event.Tick]
		public void tick()
		{
			SetAnimParameter( "opencage", UsedCage );
		}

		[Input]
		public void DisableCage()
		{
			AnimatedEntity placeholdercage = new AnimatedEntity( GetModelName() );
			placeholdercage.Transform = Transform;
			placeholdercage.SetAnimParameter( "opencage", true );
			UsedCage = true;
			Delete();
		}

		public override void TakeDamage( DamageInfo info )
		{
			base.TakeDamage( info );

			if ( info.Attacker is VRPlayer pawn || info.Attacker is BaseEnemyClass enemy )
			{
				HitsRequired--;
				if ( HitsRequired == 0 )
				{
					if ( OccupyingPlayer != null )
					{
						SetAnimParameter( "opencage", true );
						SetBodyGroup( 0, 1 );
						OccupyingPlayer.RevivePlayer( OccupyingPlayer.Name );
						OccupyingPlayer = null;
						UsedCage = true;
						OnPlayerSaved.Fire( this );
					}
				}
			}
		}
	}
}
