using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace rh
{
	public partial class NPCCorpse : AnimatedEntity
	{

		async Task DeleteBlink()
		{
			await Task.DelayRealtimeSeconds( 2.5f );
			for ( int i = 0; i < 20; i++ )
			{
				foreach ( ModelEntity item in Children )
				{
					item.RenderColor = Color.White.WithAlpha( 0f );
				}
				RenderColor = Color.White.WithAlpha( 0f );
				await Task.DelayRealtimeSeconds( 0.05f );
				foreach ( ModelEntity item in Children )
				{
					item.RenderColor = Color.White.WithAlpha( 1f );
				}
				RenderColor = Color.White.WithAlpha( 1f );
				await Task.DelayRealtimeSeconds( 0.05f );
			}
			Delete();
		}

		[Event.Tick.Server]
		void Tick()
		{
			TraceResult tr = Trace.Ray( Position, Position - Vector3.Up * 1f ).WorldOnly().Run();

			if ( !tr.Hit )
			{
				Position -= Vector3.Up * 150f * Time.Delta;
			}
		}

		public static NPCCorpse FromNPC(BaseEnemyClass npc ) {
			NPCCorpse corpse = new NPCCorpse();
			

			corpse.Position = npc.Position;
			corpse.Rotation = npc.Rotation;

			corpse.CopyFrom( npc );
			corpse.SetModel( "models/npcs/npccorpse.vmdl" );

			corpse.Scale = npc.Scale;

			if ( npc.enemyResource.Clothing.Count > 0 )
			{
				List<Clothing> clothing = new List<Clothing>();
				foreach ( var item in npc.enemyResource.Clothing )
				{
					clothing.Add( ResourceLibrary.Get<Clothing>( item ) );
				}

				ClothingContainer container = new ClothingContainer();

				foreach ( var item in clothing )
				{
					container.Clothing.Add( item );
				}

				container.DressEntity( corpse, false );
			}
			corpse.CopyMaterialGroup( npc );
			corpse.DeleteBlink();

			return corpse;
		}
	}
}
