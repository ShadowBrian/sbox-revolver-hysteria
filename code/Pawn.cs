using Sandbox;
using System;
using System.Linq;

namespace rh;

public partial class Pawn : AnimatedEntity
{
	[Net, Predicted] public PlayerPlatform platform { get; set; }
	[Net] public int PlayerIndex { get; set; }


	public override void Spawn()
	{
		base.Spawn();

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
	}

	bool Parented;

	/// <summary>
	/// Called every tick, clientside and serverside.
	/// </summary>
	public override void Simulate( Client cl )
	{
		base.Simulate( cl );
		if(platform != null && PlayerIndex != 0 && !Parented && IsServer)
		{
			if ( platform.GetAttachment( "player" + PlayerIndex ).HasValue )
			{
				Transform = platform.GetAttachment( "player" + PlayerIndex ).Value;
				SetParent( platform, "player" + PlayerIndex );
			}
			
			Parented = true;
		}
	}

	/// <summary>
	/// Called every frame on the client
	/// </summary>
	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );

		// Update rotation every frame, to keep things smooth
		//Rotation = Input.Rotation;
		//EyeRotation = Rotation;
	}
}
