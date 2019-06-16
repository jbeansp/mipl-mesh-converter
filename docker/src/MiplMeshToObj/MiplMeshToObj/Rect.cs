using System;
using System.Collections.Generic;
using System.Text;

namespace MiplMeshToObj
{
	struct Rect
	{
		public float x;
		public float y;
		public float width;
		public float height;

		public Rect(float x, float y, float width, float height)
		{
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
		}
	}
}
