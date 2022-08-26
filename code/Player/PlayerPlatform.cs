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

		public PlayerPlatform()
		{

		}
		public PlayerPlatform( string modelName )
		{
			SetModel( modelName );
		}

		/// <summary>
		/// Fired when the game starts.
		/// </summary>
		[Property( Name = "LockRotation" )]
		public bool LockRotation { get; set; }

		[Property( Name = "Path to take" )]
		public EntityTarget PathEntity { get; set; }

		/// <summary>
		/// Fired when the game starts.
		/// </summary>
		public Output OnGameStart { get; set; }

		/// <summary>
		/// Fired when the game ends without the players dying.
		/// </summary>
		public Output OnGameEndWin { get; set; }

		/// <summary>
		/// Fired when the game ends with the players dying.
		/// </summary>
		public Output OnGameEndDied { get; set; }

		[Net] public RevolverHysteriaMovementPathEntity pathent { get; set; }

		[Net] public SimplePathEnt simplepathent { get; set; }

		[Net] public bool GameHasStarted { get; set; } = false;

		[Net, Predicted] public int currentnode { get; set; } = 0;

		[Net] List<Coinslot> slots { get; set; } = new List<Coinslot>();

		List<NameplatePanel> namepanels { get; set; } = new List<NameplatePanel>();

		public override void ClientSpawn()
		{
			base.ClientSpawn();

			if ( GetAttachment( "name1" ).HasValue )
			{
				for ( int i = 1; i < 5; i++ )
				{
					NameplatePanel panel = new NameplatePanel();
					panel.Transform = GetAttachment( "name" + i ).Value;
					panel.PlayerIndex = i - 1;

					ModelEntity ent = new ModelEntity( "models/player/datascreen.vmdl" );
					ent.Transform = GetAttachment( "name" + i ).Value;
					ent.SetParent( this );

					namepanels.Add( panel );
				}
			}
		}

		[ConCmd.Server( "rh_debug_startgame" )]
		public static void DebugStartGame()
		{
			Entity.All.OfType<PlayerPlatform>().FirstOrDefault().GameHasStarted = true;
		}

		public override void Spawn()
		{
			base.Spawn();
			//SetModel( "models/player/playerplatform_4.vmdl" );
			SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

			if ( GetAttachment( "slot1" ).HasValue )
			{
				for ( int i = 1; i < 9; i++ )
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
						//slot.Scale = 0.5f;
						slots.Add( slot );
					}
				}
			}
			else
			{
				WaitForSlots();
				//GameHasStarted = true;
			}

		}

		public async Task WaitForSlots()
		{
			await Task.DelayRealtimeSeconds( 0.5f );
			if ( GetAttachment( "slot1" ).HasValue )
			{
				for ( int i = 1; i < 9; i++ )
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
						//slot.Scale = 0.5f;
						slots.Add( slot );
					}
				}
			}
			else
			{
				GameHasStarted = true;
			}

		}

		bool WaitingForInput;

		int InputsReceived = 0;

		/// <summary>
		/// Increases inputs received and compares it with the current node's InputReceiveCount. Continues if it's greater than.
		/// </summary>
		[Input]
		public void NodeContinueInput()
		{
			InputsReceived++;
			if ( InputsReceived >= (pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).InputReceiveCount )
			{
				WaitingForInput = false;
				InputsReceived = 0;
			}
		}

		/// <summary>
		/// Set a parameter to a value
		/// </summary>
		[Input]
		public void FireAnimationTrigger( string parameter )
		{
			Log.Trace( "Fired animation: " + parameter );
			bool value = parameter.Contains( "true" ) ? true : false;
			SetAnimParameter( parameter.Split( ',' )[0], value );
		}

		public float GetNodeLength( int node )
		{
			if ( pathent != null )
			{
				float distance = pathent.GetCurveLength( pathent.PathNodes[node], pathent.PathNodes[(node + 1) % pathent.PathNodes.Count], 5 );

				return distance;
			}
			else
			{
				float distance = simplepathent.GetNodeLength( simplepathent.PathNodes[node], simplepathent.PathNodes[(node + 1) % simplepathent.PathNodes.Count] );

				return distance;
			}
		}

		TimeSince TimeSinceEndNode;

		float movementProgress;

		[Event.Tick.Client]
		public void ClientTick()
		{

		}

		bool FiredArrive;

		[Event.Tick.Server]
		public void ServerTick()
		{
			if ( (Game.Current as RevolverHysteriaGame).EndTriggered )
			{
				return;
			}
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
				if ( pathent != null )
				{
					Vector3 newpos = pathent.GetPointBetweenNodes( pathent.PathNodes[0], pathent.PathNodes[(0 + 1) % pathent.PathNodes.Count], 0f );

					Vector3 lookpos = pathent.GetPointBetweenNodes( pathent.PathNodes[0], pathent.PathNodes[(0 + 1) % pathent.PathNodes.Count], 0.1f );
					if ( !LockRotation )
					{
						Rotation = Rotation.LookAt( lookpos - newpos, Vector3.Up );
					}

					Position = Vector3.Lerp( Position, newpos, 0.5f );

				}
				else
				{
					simplepathent = All.OfType<SimplePathEnt>().FirstOrDefault();
					if ( simplepathent != null )
					{
						Vector3 newpos = simplepathent.GetPointBetweenNodes( simplepathent.PathNodes[0], simplepathent.PathNodes[(0 + 1) % simplepathent.PathNodes.Count], 0f );

						Vector3 lookpos = simplepathent.GetPointBetweenNodes( simplepathent.PathNodes[0], simplepathent.PathNodes[(0 + 1) % simplepathent.PathNodes.Count], 0.1f );
						if ( !LockRotation )
						{
							Rotation = Rotation.LookAt( (lookpos - newpos).WithZ( 0 ), Vector3.Up );
						}

						Position = Vector3.Lerp( Position, newpos, 0.5f );

					}
				}
				if ( (Game.Current as RevolverHysteriaGame).VRPlayers.Count > 0 && (PlayersReady >= (Game.Current as RevolverHysteriaGame).VRPlayers.Count || PlayersReady == 4) )
				{
					GameHasStarted = true;
					TimeSinceEndNode = 0f;
					if ( pathent != null )
					{
						(pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).OnPassed.Fire( this );

						if ( (pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).WaitUntilInput )
						{
							WaitingForInput = true;
						}

						OnGameStart.Fire( this );
					}
				}

				return;
			}
			if ( PathEntity.Name != "" && pathent.IsValid() )
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

				RevolverHysteriaMovementPathNodeEntity nodeEnt = (pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity);

				float NodeTime = 1f / ((Time.Delta / GetNodeLength( currentnode )) * 50f * (nodeEnt.Speed + 1f));

				if ( nodeEnt.WaitUntilInput )
				{
					NodeTime *= 1000f;
				}

				if ( movementProgress >= 0.9f && !FiredArrive )
				{
					if ( currentnode + 1 < pathent.PathNodes.Count - 1 )
					{
						(pathent.PathNodes[currentnode + 1].Entity as RevolverHysteriaMovementPathNodeEntity).OnArrived.Fire( this, 0.1f );
					}
					FiredArrive = true;
				}

				if ( movementProgress > 1f && (TimeSinceEndNode > nodeEnt.TimeToWait + (NodeTime / 120f) || (nodeEnt.WaitUntilInput && !WaitingForInput)) )
				{
					TimeSinceEndNode = 0f;

					currentnode++;
					if ( currentnode > pathent.PathNodes.Count - 1 )
					{
						currentnode = pathent.PathNodes.Count - 1;
					}

					(pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).OnPassed.Fire( this );

					if ( (pathent.PathNodes[currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).WaitUntilInput )
					{
						WaitingForInput = true;
					}

					movementProgress = 0f;

					FiredArrive = false;
				}
				if ( movementProgress < 1f && currentnode != pathent.PathNodes.Count - 1 )
				{
					Vector3 newpos = pathent.GetPointBetweenNodes( pathent.PathNodes[currentnode], pathent.PathNodes[(currentnode + 1) % pathent.PathNodes.Count], movementProgress );
					if ( !LockRotation )
					{
						Rotation = Rotation.Slerp( Rotation, Rotation.LookAt( (newpos - Position).WithZ( 0 ), Vector3.Up ), 0.5f );
					}
					Position = Vector3.Lerp( Position, newpos, 0.5f );
				}

			}
			else
			{
				pathent = All.OfType<RevolverHysteriaMovementPathEntity>().FirstOrDefault();
			}

			if ( pathent == null && simplepathent == null )
			{
				simplepathent = All.OfType<SimplePathEnt>().FirstOrDefault();
			}

			if ( simplepathent != null )
			{
				movementProgress += (Time.Delta / GetNodeLength( currentnode )) * 50f * 0.75f;

				float NodeTime = 1f / ((Time.Delta / GetNodeLength( currentnode )) * 50f);

				float WaitTime = simplepathent.NodesWithSpawners.Contains( currentnode ) ? 10f : 0f;

				if ( movementProgress > 1f && TimeSinceEndNode > WaitTime + (NodeTime / 120f) )
				{
					TimeSinceEndNode = 0f;

					currentnode++;

					if ( currentnode > simplepathent.PathNodes.Count - 1 )
					{
						currentnode = simplepathent.PathNodes.Count - 1;
					}

					movementProgress = 0f;
				}
				if ( movementProgress < 1f && currentnode != simplepathent.PathNodes.Count - 1 )
				{
					Vector3 newpos = simplepathent.GetPointBetweenNodes( simplepathent.PathNodes[currentnode], simplepathent.PathNodes[(currentnode + 1) % simplepathent.PathNodes.Count], movementProgress );
					if ( !LockRotation )
					{
						Rotation = Rotation.Slerp( Rotation, Rotation.LookAt( (newpos - Position).WithZ( 0 ), Vector3.Up ), 0.25f );
					}
					Position = Vector3.Lerp( Position, newpos, 0.5f );
				}
			}
		}
	}
}
