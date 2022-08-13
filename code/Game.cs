using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
//
// You don't need to put things in a namespace, but it doesn't hurt.
//
namespace rh;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class RevolverHysteriaGame : Sandbox.Game
{
	public static List<string> GetMaps()
	{
		var packageTask = Package.Fetch( Global.GameIdent, true ).ContinueWith( t =>
		{
			Package package = t.Result;
			return package.GetMeta<List<string>>( "MapList" );
		} );

		return packageTask.Result;
	}

	public RevolverHysteriaGame()
	{
		Global.TickRate = 120;
	}

	[Net] public List<VRPlayer> VRPlayers { get; set; }

	[Net] public PlayerPlatform platform { get; set; }

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( Client client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		if ( client.IsUsingVr )
		{

			var VRRig = new VRPlayer();
			VRRig.Owner = Owner;
			VRRig.Predictable = true;
			client.Pawn = VRRig;
			VRPlayers.Add( VRRig );

			var pawn = new Pawn();
			client.Pawn = pawn;
			pawn.PlayerIndex = VRPlayers.IndexOf( VRRig ) + 1;

			VRRig.Owner = pawn;

			platform = All.OfType<PlayerPlatform>().FirstOrDefault();

			if ( platform != null && VRPlayers.IndexOf( VRRig ) < 4 )
			{
				pawn.platform = platform;
				if ( platform.GetAttachment( "player" + (VRPlayers.IndexOf( VRRig ) + 1) ).HasValue )
				{
					var tx = platform.GetAttachment( "player" + (VRPlayers.IndexOf( VRRig ) + 1) ).Value;
					pawn.Transform = tx;
				}
			}
			else
			{
				if ( platform.GetAttachment( "player" + ((VRPlayers.IndexOf( VRRig ) + 1) - 4) ).HasValue )
				{
					var tx = platform.GetAttachment( "player" + ((VRPlayers.IndexOf( VRRig ) + 1) - 4) ).Value;
					pawn.Transform = tx;
				}
			}

			// Get all of the spawnpoints
			/*var spawnpoints = Entity.All.OfType<SpawnPoint>();

			// chose a random one
			var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

			// if it exists, place the pawn there
			if ( randomSpawnPoint != null )
			{
				var tx = randomSpawnPoint.Transform;
				tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
				pawn.Transform = tx;
			}*/
		}

		if ( !client.IsUsingVr )
		{

			var pawn = new FlatPawn();
			client.Pawn = pawn;

			platform = All.OfType<PlayerPlatform>().FirstOrDefault();

			if ( platform != null )
			{

				pawn.Position = platform.Position + platform.Rotation.Forward * 100f + Vector3.Up * 40f;
				pawn.Rotation = Rotation.LookAt( pawn.Position - platform.Position );

			}
			else
			{
				pawn.Position = Vector3.Up * 64f;
			}
		}
	}

	public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
	{
		base.ClientDisconnect( cl, reason );
		for ( int i = 0; i < VRPlayers.Count; i++ )
		{
			if ( VRPlayers[i].Client == cl )
			{
				VRPlayers[i].LH.Delete();
				VRPlayers[i].RH.Delete();
				VRPlayers[i].HeadEnt.Delete();
				VRPlayers[i].Delete();
				VRPlayers.RemoveAt( i );
				break;
			}
		}
	}

	[Net] public bool EndTriggered { get; set; } = false;
	[Net] TimeSince TimeSinceEnded { get; set; }

	[Net] bool FirstPlayerArrived { get; set; }

	[Net] TimeSince TimeSinceFirstPlayer { get; set; }

	RHVotingBoard board;

	bool DebugMode = false;

	public override void OnVoicePlayed( Client cl )
	{
		base.OnVoicePlayed( cl );
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		int deadplayers = 0;

		if ( (VRPlayers.Count > 0 || DebugMode) && !FirstPlayerArrived )
		{
			TimeSinceFirstPlayer = 0f;
			FirstPlayerArrived = true;
		}

		if ( !FirstPlayerArrived || TimeSinceFirstPlayer < 0.5f )
		{
			return;
		}

		foreach ( var vrplayer in VRPlayers )
		{
			vrplayer.Simulate( cl );
			if ( vrplayer.HeadEnt.IsValid() && vrplayer.HeadEnt.HitPoints <= 0 )
			{
				deadplayers++;
			}
		}

		if ( (deadplayers == VRPlayers.Count && IsServer && ((platform.GameHasStarted && VRPlayers.Count > 0) || DebugMode)) || (platform.pathent.IsValid() && (platform.currentnode == platform.pathent.PathNodes.Count - 1 && !(platform.pathent.PathNodes[platform.currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).AlternativePathEnabled) && IsServer) && !EndTriggered )
		{
			TimeSinceEnded = 0f;
			foreach ( var vrplayer in VRPlayers )
			{
				GameServices.SubmitScore( vrplayer.Client.PlayerId, vrplayer.Client.GetInt( "score" ) );
				if ( vrplayer.HeadEnt.HitPoints <= 0 )
				{
					vrplayer.RevivePlayer( vrplayer.Name );
				}
			}

			if ( platform.currentnode == platform.pathent.PathNodes.Count - 1 && !(platform.pathent.PathNodes[platform.currentnode].Entity as RevolverHysteriaMovementPathNodeEntity).AlternativePathEnabled )
			{
				platform.OnGameEndWin.Fire( platform );
			}
			else
			{
				platform.OnGameEndDied.Fire( platform );
			}

			board = new RHVotingBoard();

			EndTriggered = true;
		}

		if ( EndTriggered && TimeSinceEnded > 30f && IsServer )
		{
			Global.ChangeLevel( board.ReturnMostVotedMap() );
		}


		if ( EndTriggered && TimeSinceEnded < 10f && IsClient )
		{
			foreach ( var vrplayer in VRPlayers )
			{
				vrplayer.Simulate( cl );
				vrplayer.HeadEnt.ShowDeathScreen();
			}
		}
	}

	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );

		foreach ( var vrplayer in VRPlayers )
		{
			vrplayer.FrameSimulate( cl );
		}
	}

	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		if ( Local.Pawn != null )
		{
			// VR anchor default is at the pawn's location
			//VR.Anchor = Local.Pawn.Transform;

			Local.Pawn.PostCameraSetup( ref camSetup );
		}

		if ( Input.VR.IsActive )
			camSetup.ZNear = 2.5f;
		//
		// Position any viewmodels
		//
		BaseViewModel.UpdateAllPostCamera( ref camSetup );

		//CameraModifier.Apply( ref camSetup );
	}
}
