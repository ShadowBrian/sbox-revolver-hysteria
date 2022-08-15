﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

namespace rh
{
	public static class ShuffleExt
	{
		public static IEnumerable<T> Shuffle<T>( this IEnumerable<T> source, Random rng )
		{
			T[] elements = source.ToArray();
			for ( int i = elements.Length - 1; i >= 0; i-- )
			{
				// Swap element "i" with a random earlier element it (or itself)
				// ... except we don't really need to swap it fully, as we can
				// return it immediately, and afterwards it's irrelevant.
				int swapIndex = rng.Next( i + 1 );
				yield return elements[swapIndex];
				elements[swapIndex] = elements[i];
			}
		}
	}
	public partial class ProceduralRHEnts : Entity
	{

		[Net] List<Vector3> MovementhPath { get; set; } = new List<Vector3>();

		[Net] public Transform StartLocation { get; set; }
		[Net] public Transform EndLocation { get; set; }

		List<int> EnemySpawnersToPlace = new List<int> { 2, 3, 3, 4, 3, 4, 5, 4, 5, 5, 6, 7, 8 };

		int Difficultygradientspot;

		public override void Spawn()
		{
			base.Spawn();

			var spawnpoints = Entity.All.OfType<SpawnPoint>();

			Rand.SetSeed( Global.MapName.Length );

			// chose a random one
			var randomSpawnPoint = spawnpoints.Shuffle<SpawnPoint>( new Random( Global.MapName.Length + 12 ) ).FirstOrDefault();

			StartLocation = randomSpawnPoint.Transform;

			IEnumerable<SpawnPoint> allspawns = spawnpoints.Shuffle<SpawnPoint>( new Random( Global.MapName.Length + 1201 ) );

			float MinimumDistanceEnd = 0f;

			int iterations = 0;

			for ( int i = 0; i < allspawns.Count() - 1; i++ )
			{
				float dist = Vector3.DistanceBetween( StartLocation.Position, allspawns.ElementAt( i ).Position );
				if ( dist > MinimumDistanceEnd )
				{
					randomSpawnPoint = allspawns.ElementAt( i );
					MinimumDistanceEnd = dist;
				}
			}


			if ( iterations >= allspawns.Count() - 1 )
			{
				Log.Trace( "Couldn't find far away enough end point! Trying to generate one." );
				Transform newpos = new Transform();
				newpos.Position = StartLocation.Position;
				newpos.Position += Vector3.Random * 4500f;

				newpos.Position = newpos.Position.WithZ( randomSpawnPoint.Position.z );

				Vector3 finalpos = Vector3.Zero;

				NavArea.GetClosestNav( newpos.Position, NavAgentHull.Agent2, GetNavAreaFlags.NoFlags, ref finalpos );

				newpos.Position = finalpos;

				EndLocation = newpos;
			}
			else
			{
				EndLocation = randomSpawnPoint.Transform;
			}

			BuildPath();

			if ( MovementhPath.Count > 0 )
			{
				AddPathEntity();
			}

			AddPlatform();

			FigureOutEnemySpawns();
		}

		public void BuildPath()
		{
			NavPathBuilder builder = NavMesh.PathBuilder( StartLocation.Position );
			builder.WithAgentHull( NavAgentHull.Agent2 );

			NavPath path = builder.Build( EndLocation.Position );

			if ( path != null && path.Segments != null && path.Segments.Count > 1 )
			{
				foreach ( NavPathSegment seg in path.Segments )
				{
					MovementhPath.Add( seg.Position + Vector3.Up * 24f );
				}
			}
			else
			{
				MovementhPath.Clear();
			}
		}

		SimplePathEnt pathEnt;

		public void AddPathEntity()
		{
			pathEnt = new SimplePathEnt();
			foreach ( Vector3 seg in MovementhPath )
			{
				Transform nodetrans = new Transform();
				nodetrans.Position = seg;
				if ( pathEnt.PathNodes.Count > 0 )
				{
					nodetrans.Rotation = Rotation.LookAt( nodetrans.Position - pathEnt.PathNodes[pathEnt.PathNodes.Count - 1].Position, Vector3.Up );
				}
				else
				{
					nodetrans.Rotation = StartLocation.Rotation;
				}
				pathEnt.PathNodes.Add( nodetrans );
			}

			for ( int i = 1; i < pathEnt.PathNodes.Count - 2; i++ )
			{
				Transform trans = pathEnt.PathNodes[i];
				Vector3 prevlook = pathEnt.PathNodes[i].Position - pathEnt.PathNodes[i - 1].Position;

				Vector3 nextlook = pathEnt.PathNodes[i + 1].Position - pathEnt.PathNodes[i].Position;

				trans.Rotation = Rotation.LookAt( (prevlook + (nextlook / 2f)) / 2f, Vector3.Up );
				pathEnt.PathNodes[i] = trans;
			}
		}

		public void AddPlatform()
		{
			PlayerPlatform plat = new PlayerPlatform( "models/player/playerplatform_1.vmdl" );

			plat.Position = StartLocation.Position;

			plat.Rotation = StartLocation.Rotation;

			if ( plat.GetAttachment( "backupscoreboard" ).HasValue )
			{
				RHWorldBoard scoreboard = new RHWorldBoard();
				scoreboard.Transform = plat.GetAttachment( "backupscoreboard" ).Value;
				scoreboard.SetParent( plat, "backupscoreboard" );
				scoreboard.Scale = 0.1f;
			}
		}

		Vector3 LastEnemySpawnNodePosition;

		public void FigureOutEnemySpawns()
		{
			Rand.SetSeed( Global.MapName.Length );
			bool failedspawn = false;
			foreach ( var node in pathEnt.PathNodes )
			{
				if ( Vector3.DistanceBetween( LastEnemySpawnNodePosition, node.Position ) > 500f )
				{
					for ( int i = 0; i < EnemySpawnersToPlace[Difficultygradientspot]; i++ )
					{
						EnemySpawner spawner = new EnemySpawner();
						spawner.AssociatedNodeNumber = pathEnt.PathNodes.IndexOf( node ) - 1;


						spawner.DirectTargetMode = true;

						Vector3 finalpos = Vector3.Zero;

						Vector3 spawnpos = node.Position + Vector3.Random * 2500f;

						while ( Vector3.DistanceBetween( spawnpos, node.Position ) < 500f )
						{
							spawnpos = node.Position + Vector3.Random * 2500f;
						}

						spawnpos = spawnpos.WithZ( node.Position.z );

						TraceResult FloorCheck = Trace.Ray( spawnpos, spawnpos - Vector3.Up * 1000f ).WorldOnly().Run();
						if ( FloorCheck.Hit )
						{
							spawnpos = spawnpos.WithZ( FloorCheck.EndPosition.z );

							NavArea.GetClosestNav( spawnpos, NavAgentHull.Default, GetNavAreaFlags.NoFlags, ref finalpos );
						}
						else
						{
							finalpos = Vector3.Zero;
						}

						if ( finalpos != Vector3.Zero )
						{
							spawner.Position = finalpos.WithZ( node.Position.z );
							finalpos = node.Position;

							EnemySpawnerNode enemynode = new EnemySpawnerNode();

							enemynode.SetParent( spawner );

							enemynode.Position = finalpos;

							LastEnemySpawnNodePosition = node.Position;

							pathEnt.NodesWithSpawners.Add( pathEnt.PathNodes.IndexOf( node ) - 1 );
						}
						else
						{
							spawner.Delete();
							Log.Trace( "EnemySpawner Failed" );
							failedspawn = true;
						}
					}

					if ( !failedspawn )
					{
						Difficultygradientspot++;
					}

					if ( Difficultygradientspot >= EnemySpawnersToPlace.Count - 1 )
					{
						Difficultygradientspot = EnemySpawnersToPlace.Count - 1;
					}
				}
			}
		}

		/*[Event.Tick.Server]
		public void Tick()
		{
			if ( pathEnt != null )
			{
				for ( int i = 0; i < pathEnt.PathNodes.Count - 1; i++ )
				{
					//DebugOverlay.Line( pathEnt.PathNodes[i].Position, pathEnt.PathNodes[i + 1].Position, Color.Red );
					DebugOverlay.Axis( pathEnt.PathNodes[i].Position, pathEnt.PathNodes[i].Rotation );

					Vector3 lastPos = pathEnt.PathNodes[i].Position;

					for ( int i2 = 1; i2 <= 10; i2++ ) // Starting from 1 because i = 0 is start.Position
					{
						var lerpPos = pathEnt.GetPointBetweenNodes( pathEnt.PathNodes[i], pathEnt.PathNodes[i + 1], i2 / 10f );

						DebugOverlay.Line( lerpPos, lastPos, Color.Green );

						lastPos = lerpPos;
					}


					//DebugOverlay.Line( MovementhPath[i], MovementhPath[i] + Vector3.Up * 10f, Color.Red );
				}
			}
		}*/
	}
}
