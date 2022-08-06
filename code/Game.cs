using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
public partial class MyGame : Sandbox.Game
{
	public MyGame()
	{

	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( Client client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		if ( client.IsUsingVr )
		{
			var pawn = new VRPlayer();
			client.Pawn = pawn;

			// Get all of the spawnpoints
			var spawnpoints = Entity.All.OfType<SpawnPoint>();

			// chose a random one
			var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

			// if it exists, place the pawn there
			if ( randomSpawnPoint != null )
			{
				var tx = randomSpawnPoint.Transform;
				tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
				pawn.Transform = tx;
			}
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
