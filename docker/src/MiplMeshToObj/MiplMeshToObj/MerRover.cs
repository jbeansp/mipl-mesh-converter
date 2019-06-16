using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MiplMeshToObj
{
	class MerRover : IRover
	{
		public string Name => "MER";

		public bool ShouldConvertToOsgx(string meshPath)
		{

			// Convert to osgx first for all.pfb meshes except the HiRise meshes.MER HiRise meshes are single quad meshes, which the
			// code to convert osgx to obj doesn't handle well.  Also, there are no LODs in the HiRise meshes, so there's no need to convert
			// to osgx anyway.

			return !IsHiriseMesh(meshPath);

		}


		//e.g., normal:  1mesh_3187_F_168_FFL_1699_v1.iv
		// normal:  1mesh_4346x_RFN_198_RSL_245x_v1.iv

		// not normal:  1mesh_2969_H_163_R1m_565_slope_ntilt_n5_15_deg_v1.iv
		// not normal:  1mesh_2775_H_162_R25C_51_north_slope.iv  
		// not normal:  1mesh_3625_H_184_R25C_313_v1.iv 
		private static readonly Regex cameraCodeRegex = new Regex(@"[12]mesh_[0-9]+[\S]?_([a-zA-Z]+)_\S*$", RegexOptions.IgnoreCase);

		private bool IsHiriseMesh(string meshFilePath)
		{
			Match matches = cameraCodeRegex.Match(Path.GetFileName(meshFilePath));

			if (!matches.Success)
			{
				throw new ArgumentException($"MER regex was unable to match against filename for {meshFilePath}.  Examples of filenames that work: 1mesh_2969_H_* or 1mesh_4346x_RFN_*");
			}

			if (matches.Groups.Count < 2)
			{
				throw new ArgumentException($"MER regex was unable to find a camera code in the filename for {meshFilePath}.  Examples of filenames that work: 1mesh_2969_H_* or  or 1mesh_4346x_RFN_*");
			}

			string cameraCode = matches.Groups[1].Value;

			return cameraCode.ToUpper() == "H";
		}

	}
}
