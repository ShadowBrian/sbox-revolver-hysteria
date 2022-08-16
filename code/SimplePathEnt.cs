using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace rh
{
	public partial class SimplePathEnt : Entity
	{
		[Net] public List<Transform> PathNodes { get; set; } = new List<Transform>();

		[Net] public List<int> NodesWithSpawners { get; set; } = new List<int>();

		public float GetNodeLength( Transform start, Transform end )
		{
			Vector3 lastPos = start.Position;

			int segments = 6;

			float length = 0;
			for ( int i = 1; i <= segments; i++ ) // Starting from 1 because i = 0 is start.Position
			{
				var lerpPos = GetPointBetweenNodes( start, end, (float)i / segments );

				length += (lerpPos - lastPos).Length;

				lastPos = lerpPos;
			}
			return length;
		}

		public Vector3 GetPointBetweenNodes( Transform start, Transform end, float t )
		{
			Vector3 pos;
			Vector3 tanOut;

			float mult = Vector3.DistanceBetween( start.Position, end.Position ) / 3f;

			pos = start.Position;
			tanOut = start.Position + start.Rotation.Forward * mult;

			tanOut = tanOut.WithZ( start.Position.z );


			Vector3 posNext;
			Vector3 tanInNext;

			posNext = end.Position;
			tanInNext = end.Position - end.Rotation.Forward * mult;

			tanInNext = tanInNext.WithZ( end.Position.z );

			Vector3 lerp1 = pos.LerpTo( tanOut, t );
			Vector3 lerp2 = tanOut.LerpTo( tanInNext, t );
			Vector3 lerp3 = tanInNext.LerpTo( posNext, t );
			Vector3 lerpAlmost1 = lerp1.LerpTo( lerp2, t );
			Vector3 lerpAlmost2 = lerp2.LerpTo( lerp3, t );

			return lerpAlmost1.LerpTo( lerpAlmost2, t );
		}
	}
}
