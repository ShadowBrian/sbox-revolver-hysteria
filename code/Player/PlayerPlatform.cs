using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using SandboxEditor;
namespace rh
{
	[Library( "ent_rh_playerplatform_4" )]
	[HammerEntity]
	[Model( Model = "models/player/playerplatform_4.vmdl" )]
	public partial class PlayerPlatform : AnimatedEntity
	{
		[Property( Name = "Path to take" )]
		public EntityTarget PathEntity { get; set; }

		RevolverHysteriaMovementPathEntity pathent;

		[Net] public bool GameHasStarted { get; set; } = false;

		[Net, Predicted] public int currentnode { get; set; } = 0;

		[Net] List<Coinslot> slots { get; set; } = new List<Coinslot>();

		public override void Spawn()
		{
			base.Spawn();
			//SetModel( "models/player/playerplatform_4.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

			if ( GetAttachment( "slot1" ).HasValue )
			{
				for ( int i = 1; i < (Game.Current as RevolverHysteriaGame).VRPlayers.Count + 1; i++ )
				{
					if ( i < 5 )
					{
						Coinslot slot = new Coinslot();
						slot.Transform = GetAttachment( "slot" + i ).Value;
						slot.SetParent( this, "slot" + i );
						slot.PlayerIndex = i;
						slots.Add( slot );
					}
					else
					{
						Coinslot slot = new Coinslot();
						slot.Transform = GetAttachment( "slot" + (i - 4) ).Value;
						slot.SetParent( this, "slot" + (i - 4) );
						slot.PlayerIndex = i;
						slots.Add( slot );
					}
				}
			}
			else
			{
				GameHasStarted = true;
			}
		}

		public float GetNodeLength( int node )
		{
			float distance = pathent.GetCurveLength( pathent.PathNodes[node], pathent.PathNodes[(node + 1) % pathent.PathNodes.Count], 5 );

			return distance;
		}

		TimeSince TimeSinceEndNode;

		float movementProgress;

		[Event.Tick.Server]
		public void ServerTick()
		{
			if ( !GameHasStarted )
			{
				int PlayersReady = 0;
				foreach ( Coinslot slot in slots )
				{
					if ( slot.InsertedCoin )
					{
						PlayersReady++;
					}
				}
				if ( PathEntity.Name != "" )
				{
					if ( pathent == null )
					{
						pathent = FindByName( PathEntity.Name, All.OfType<RevolverHysteriaMovementPathEntity>().FirstOrDefault() ) as RevolverHysteriaMovementPathEntity;
					}
				}
				else
				{
					pathent = All.OfType<RevolverHysteriaMovementPathEntity>().FirstOrDefault();
				}
				Vector3 newpos = pathent.GetPointBetweenNodes( pathent.PathNodes[0], pathent.PathNodes[(0 + 1) % pathent.PathNodes.Count], 0f );

				Vector3 lookpos = pathent.GetPointBetweenNodes( pathent.PathNodes[0], pathent.PathNodes[(0 + 1) % pathent.PathNodes.Count], 0.1f );
				Rotation = Rotation.LookAt( lookpos - newpos, Vector3.Up );

				Position = Vector3.Lerp( Position, newpos, 0.5f );

				if ( (Game.Current as RevolverHysteriaGame).VRPlayers.Count > 0 && (PlayersReady >= (Game.Current as RevolverHysteriaGame).VRPlayers.Count || PlayersReady == 4) )
				{
					GameHasStarted = true;
					TimeSinceEndNode = 0f;
					(pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).OnPassed.Fire( this );
				}

				return;
			}
			if ( PathEntity.Name != "" )
			{
				if ( pathent == null )
				{
					pathent = FindByName( PathEntity.Name, All.OfType<RevolverHysteriaMovementPathEntity>().FirstOrDefault() ) as RevolverHysteriaMovementPathEntity;
				}
				movementProgress += (Time.Delta / GetNodeLength( currentnode )) * 50f * ((pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).Speed + 1f);

				bool AltPath = ((pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).AlternativePathEnabled == true);

				if ( AltPath )
				{
					Entity ent = Entity.FindByName( (pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).AlternativeNodeForwards.Name );
					if ( ent != null )
					{
						pathent = ent.Parent as RevolverHysteriaMovementPathEntity;

						for ( int i = 0; i < pathent.PathNodes.Count; i++ )
						{
							if ( pathent.PathNodes[i].Entity == ent )
							{
								currentnode = i;
								break;
							}
						}
					}
				}

				float NodeTime = 1f / ((Time.Delta / GetNodeLength( currentnode )) * 50f * ((pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).Speed + 1f));

				if ( movementProgress > 1f && TimeSinceEndNode > (pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).TimeToWait + (NodeTime / 120f) )
				{
					TimeSinceEndNode = 0f;

					currentnode++;
					if ( currentnode > pathent.PathNodes.Count - 1 )
					{
						currentnode = pathent.PathNodes.Count - 1;
					}

					(pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).OnPassed.Fire( this );

					movementProgress = 0f;
				}
				if ( movementProgress < 1f && currentnode != pathent.PathNodes.Count - 1 )
				{
					Vector3 newpos = pathent.GetPointBetweenNodes( pathent.PathNodes[currentnode], pathent.PathNodes[(currentnode + 1) % pathent.PathNodes.Count], movementProgress );
					Rotation = Rotation.Slerp( Rotation, Rotation.LookAt( newpos - Position, Vector3.Up ), 0.5f );
					Position = Vector3.Lerp( Position, newpos, 0.5f );
				}

			}
			else
			{
				pathent = All.OfType<RevolverHysteriaMovementPathEntity>().FirstOrDefault();
			}
		}
	}
}
