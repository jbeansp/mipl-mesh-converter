using System;
using System.Collections.Generic;
using System.Text;

namespace MiplMeshToObj
{
	class MslRover : IRover
	{
		public string Name => "MSL";

		/// <summary>
		/// All MIPL .iv meshes work best converting to osgx first.
		/// </summary>
		public bool ShouldConvertToOsgx(string meshPath)
		{
			return true;
		}
	}
}
