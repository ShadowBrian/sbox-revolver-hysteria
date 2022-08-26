using System;
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

		List<int> EnemySpawnersToPlace = new List<int> { 5, 5, 6, 6, 6, 7, 7, 7, 7, 8, 8, 8, 9, 9, 9 };

		int Difficultygradientspot;

		public override void Spawn()
		{
			base.Spawn();

			var spawnpoints = Entity.All.OfType<SpawnPoint>();

			Rand.SetSeed( Global.MapName.Length + 18 );

			// chose a random one
			Entity randomSpawnPoint = spawnpoints.Shuffle<SpawnPoint>( new Random( Global.MapName.Length + 2 ) ).FirstOrDefault();

			StartLocation = randomSpawnPoint.Transform;

			IEnumerable<SpawnPoint> allspawns = spawnpoints.Shuffle<SpawnPoint>( new Random( Global.MapName.Length + 1206 ) );
			int iterations = 0;
			if ( allspawns.Count() > 5 )
			{
				float MinimumDistanceEnd = 0f;

				for ( int i = 0; i < allspawns.Count() - 1; i++ )
				{
					float dist = Vector3.DistanceBetween( StartLocation.Position, allspawns.ElementAt( i ).Position );
					//Log.Trace( "new max: " + dist );
					if ( dist > MinimumDistanceEnd )
					{
						randomSpawnPoint = allspawns.ElementAt( i );
						MinimumDistanceEnd = dist;
						iterations++;
					}
				}
			}
			else
			{
				IEnumerable<PointLightEntity> alllights = Entity.All.OfType<PointLightEntity>().Shuffle( new Random( Global.MapName.Length + 2 ) );

				float MinimumDistanceEnd = 0f;

				for ( int i = 0; i < alllights.Count() - 1; i++ )
				{
					float dist = Vector3.DistanceBetween( StartLocation.Position, alllights.ElementAt( i ).Position );
					//Log.Trace( "new max: " + dist );
					if ( dist > MinimumDistanceEnd )
					{
						randomSpawnPoint = alllights.ElementAt( i );
						MinimumDistanceEnd = dist;
						iterations++;
					}
				}
			}


			if ( Vector3.DistanceBetween( randomSpawnPoint.Position, StartLocation.Position ) < 2500f )
			{
				Log.Trace( "Couldn't find far away enough end point! Trying to generate one." );
				Transform newpos = new Transform();
				newpos.Position = StartLocation.Position;
				Vector3 addvec = Vector3.Random * 2055f;

				while ( addvec.Length < 1000f )
				{
					addvec = Vector3.Random * 2051f;
				}

				newpos.Position += addvec;

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

		public void PathTooDullRedo()
		{
			Log.Trace( "Path was too dull! Trying to generate a new one." );
			Transform newpos = new Transform();
			newpos.Position = StartLocation.Position;
			Vector3 addvec = Vector3.Random * 4555f;

			while ( addvec.Length < 3500f )
			{
				addvec = Vector3.Random * 4551f;
			}

			newpos.Position += addvec;

			newpos.Position = newpos.Position.WithZ( EndLocation.Position.z );

			Vector3 finalpos = Vector3.Zero;

			NavArea.GetClosestNav( newpos.Position, NavAgentHull.Agent2, GetNavAreaFlags.NoFlags, ref finalpos );

			newpos.Position = finalpos;

			EndLocation = newpos;
		}

		public void BuildPath()
		{
			NavPathBuilder builder = NavMesh.PathBuilder( StartLocation.Position ).WithPathSpeed( 50 ).WithStartAcceleration( StartLocation.Rotation.Forward * 50f );
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

			if ( MovementhPath.Count < 5 )
			{
				MovementhPath.Clear();
				PathTooDullRedo();
				BuildPath();
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

						TraceResult FloorCheck = Trace.Ray( spawnpos + Vector3.Up, spawnpos - Vector3.Up * 1000f ).WorldOnly().Run();
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
							spawner.Position = finalpos;
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

		public bool showpath;

		[ConCmd.Server( "rh_debug_showsimplepath" )]
		public static void ShowDebugPath()
		{
			ProceduralRHEnts ent = Entity.All.OfType<ProceduralRHEnts>().FirstOrDefault();

			if ( ent.IsValid() )
			{
				ent.showpath = true;
			}
		}

		[Event.Tick.Server]
		public void Tick()
		{
			if ( showpath && pathEnt != null )
			{
				for ( int i = 0; i < pathEnt.PathNodes.Count - 1; i++ )
				{
					//DebugOverlay.Line( pathEnt.PathNodes[i].Position, pathEnt.PathNodes[i + 1].Position, Color.Red );
					DebugOverlay.Axis( pathEnt.PathNodes[i].Position, pathEnt.PathNodes[i].Rotation );

					Vector3 lastPos = pathEnt.PathNodes[i].Position;

					for ( int i2 = 1; i2 <= 10; i2++ ) // Starting from 1 because i = 0 is start.Position
					{
						var lerpPos = pathEnt.GetPointBetweenNodes( pathEnt.PathNodes[i], pathEnt.PathNodes[i + 1], i2 / 10f );

						DebugOverlay.Line( lerpPos, lastPos, Color.Green, 0f, false );

						lastPos = lerpPos;
					}


					//DebugOverlay.Line( MovementhPath[i], MovementhPath[i] + Vector3.Up * 10f, Color.Red );
				}
			}
		}
	}
}
