using System;
using System.Collections.Generic;
using System.Text;

namespace MiplMeshToObj
{
	interface IRover
	{
		string Name { get; }

		bool ShouldConvertToOsgx(string meshPath);
	}
}
