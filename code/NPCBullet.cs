using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace rh
{
	public partial class NPCBullet : AnimatedEntity
	{
		Particles system;
		public override void Spawn()
		{
			SetModel( "models/npcs/bullet/bullet.vmdl" );
			system = Particles.Create( "particles/tracer.standard.vpcf" );

			DeleteAsync( 20f );
		}

		[Event.Tick.Server]
		public void Tick()
		{
			system?.SetPosition( 0, Position );
			system?.SetPosition( 1, Position + Rotation.Forward * 20f );

			Position += Rotation.Forward * Time.Delta * 250f;

			if ( Owner.IsValid() )
			{

				TraceResult result = Trace.Ray( Position, Position + Rotation.Forward * 10f ).Ignore( Owner ).Run();

				if ( result.StartedSolid )
				{
					Delete();
				}

				if ( result.Hit )
				{
					if ( result.Entity is VRHead head )
					{
						result.Surface.DoBulletImpact( result );

						var damageInfo = DamageInfo.FromBullet( result.EndPosition, Rotation.Forward * 100, 10f )
								.UsingTraceResult( result )
								.WithAttacker( Owner )
								.WithWeapon( this );

						result.Entity.TakeDamage( damageInfo );
					}

					Delete();
				}
			}
			else
			{
				TraceResult result = Trace.Ray( Position, Position + Rotation.Forward * 10f ).Run();

				if ( result.StartedSolid )
				{
					Delete();
				}

				if ( result.Hit )
				{
					if ( result.Entity is VRHead head )
					{
						result.Surface.DoBulletImpact( result );

						var damageInfo = DamageInfo.FromBullet( result.EndPosition, Rotation.Forward * 100, 10f )
								.UsingTraceResult( result )
								.WithAttacker( Owner )
								.WithWeapon( this );

						result.Entity.TakeDamage( damageInfo );
					}

					Delete();
				}
			}
		}
	}
}
