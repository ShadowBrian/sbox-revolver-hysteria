using Editor;
using Sandbox;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace rh
{
	/// <summary>
	/// A movement path. Compiles each node as its own entity, allowing usage of inputs and outputs such as OnPassed.<br/>
	/// This entity can be used with entities like ent_path_platform.
	/// </summary>
	[Library( "rh_movement_path" )]
	[HammerEntity]
	[Path( "rh_movement_path_node", true )]
	[Title( "Revolver Hysteria Movement Path" ), Category( "Gameplay" ), Icon( "moving" )]
	public partial class RevolverHysteriaMovementPathEntity : GenericPathEntity
	{
		public RevolverHysteriaMovementPathEntity()
		{

		}

		public RevolverHysteriaMovementPathEntity( List<BasePathNode> nodes, int ID )
		{
			PathNodes = nodes;
			HammerID = ID;
		}

		public void SetNodes( List<BasePathNode> nodes )
		{
			PathNodes = nodes;
		}

		public override void Spawn()
		{
			base.Spawn();
			SpawnAsync();
		}

		public async Task SpawnAsync()
		{
			await Task.DelayRealtimeSeconds( 0.1f );

			foreach ( var item in PathNodes )
			{
				item.Entity.SetParent( this );
			}
		}

		public override void DrawPath( int segments, bool drawTangents = false )
		{
			base.DrawPath( segments, drawTangents );

			// Draw the looped part of the path
			if ( Looped )
			{
				BasePathNode start = PathNodes.Last();
				BasePathNode end = PathNodes.First();

				Vector3 nodePos = start.Entity.IsValid() ? start.Entity.Position : Transform.PointToWorld( start.Position );
				for ( int i = 1; i <= segments; i++ ) // Starting from i = 1 because i = 0 is start.Position
				{
					var lerpPos = GetPointBetweenNodes( start, end, (float)i / segments );

					if ( i % 2 == 0 ) DebugOverlay.Line( nodePos, lerpPos, Color.Green.Darken( 0.5f ) );

					nodePos = lerpPos;
				}
			}

			// Draw the path changing routes
			foreach ( var node in PathNodes )
			{
				if ( node.Entity is not RevolverHysteriaMovementPathNodeEntity mpNode ) continue;

				float darkenAmt = mpNode.AlternativePathEnabled ? 0 : 0.5f;

				// The forwards node
				if ( mpNode.AlternativeNodeForwards.GetTargets( this ).FirstOrDefault() is RevolverHysteriaMovementPathNodeEntity nodeNext )
				{
					DebugOverlay.Sphere( nodeNext.Position, 4, Color.Orange.Darken( darkenAmt ) );

					var targetPath = nodeNext.PathEntity as GenericPathEntity;
					BasePathNode nodeNextThing = targetPath.PathNodes.First();
					foreach ( var n in targetPath.PathNodes )
					{
						if ( n.Entity == nodeNext ) nodeNextThing = n;
					}

					Vector3 nodePos = node.Entity.IsValid() ? node.Entity.Position : Transform.PointToWorld( node.Position );
					for ( int i = 1; i <= segments; i++ )
					{
						var lerpPos = GetPointBetweenNodes( node, nodeNextThing, (float)i / segments );

						DebugOverlay.Line( nodePos, lerpPos, Color.Yellow.Darken( darkenAmt ) );

						nodePos = lerpPos;
					}
				}

				// The backwards node
				if ( mpNode.AlternativeNodeBackwards.GetTargets( this ).FirstOrDefault() is RevolverHysteriaMovementPathNodeEntity nodePrev )
				{
					DebugOverlay.Sphere( nodePrev.Position, 4, Color.Orange.Darken( darkenAmt ) );

					var targetPath = nodePrev.PathEntity as GenericPathEntity;
					BasePathNode nodeNextThing = targetPath.PathNodes.First();
					foreach ( var n in targetPath.PathNodes )
					{
						if ( n.Entity == nodePrev ) nodeNextThing = n;
					}

					Vector3 nodePos = node.Entity.IsValid() ? node.Entity.Position : Transform.PointToWorld( node.Position );
					for ( int i = 1; i <= segments; i++ )
					{
						var lerpPos = GetPointBetweenNodes( node, nodeNextThing, (float)i / segments, true );

						DebugOverlay.Line( nodePos, lerpPos, Color.Cyan.Darken( darkenAmt ) );

						nodePos = lerpPos;
					}
				}
			}
		}

		/// <summary>
		/// Whether the path is looped or not.
		/// </summary>
		[Property]
		public bool Looped { get; set; } = false;
	}

	/// <summary>
	/// A movement path node.
	/// </summary>
	[Library( "rh_movement_path_node" )]
	public partial class RevolverHysteriaMovementPathNodeEntity : BasePathNodeEntity
	{
		// TODO: Forward direction for the platform

		/// <summary>
		/// When passing this node, the moving entity will have its speed multiplied additively to this value. 0 or less mean do not change. It's basically Speed *= (Value + 1)
		/// </summary>
		[Property]
		public float Speed { get; set; } = 0;

		/// <summary>
		/// Time to wait before moving on to the next node, this applies to the end of the node not this node.
		/// </summary>
		[Property]
		public float TimeToWait { get; set; } = 30;

		/// <summary>
		/// Wait until the platform receives a NodeContinueInput input?
		/// </summary>
		[Property]
		public bool WaitUntilInput { get; set; }

		/// <summary>
		/// How many times should the platform receive a NodeContinueInput input before moving on? 
		/// Think: each enemy spawner associated to a node fires a "LastEnemySpawned" output into the platform.
		/// Could also make a bunch of breakable props shaped like a blockade that each fire the NodeContinueInput when broken.
		/// </summary>
		[Property]
		public int InputReceiveCount { get; set; } = 1;

		/// <summary>
		/// Whether the alternative path is enabled or not.
		/// </summary>
		[Property, Category( "Alternative Path" ), Title( "Enabled" )]
		public bool AlternativePathEnabled { get; set; }

		/// <summary>
		/// Alternative node when moving forwards, for path changing.
		/// </summary>
		[Property, Category( "Alternative Path" ), Title( "Forward Path" )]
		public EntityTarget AlternativeNodeForwards { get; set; }

		/// <summary>
		/// Alternative node when moving backwards, for path changing.
		/// </summary>
		[Property, Category( "Alternative Path" ), Title( "Backward Path" )]
		public EntityTarget AlternativeNodeBackwards { get; set; }

		/// <summary>
		/// Fired when an entity passes this node, depending on the entity implementation.
		/// </summary>
		public Output OnPassed { get; set; }

		/// <summary>
		/// Fired when an entity arrives at this node.
		/// </summary>
		public Output OnArrived { get; set; }

		/// <summary>
		/// Enables the alternative path.
		/// </summary>
		[Input]
		public void EnableAlternativePath()
		{
			AlternativePathEnabled = true;
		}

		/// <summary>
		/// Disables the alternative path.
		/// </summary>
		[Input]
		public void DisablesAlternativePath()
		{
			AlternativePathEnabled = false;
		}

		/// <summary>
		/// Toggles the alternative path.
		/// </summary>
		[Input]
		public void ToggleAlternativePath()
		{
			AlternativePathEnabled = !AlternativePathEnabled;
		}
	}
}
