using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;

namespace MiplMeshToObj
{
	class MeshImageTile
	{
		private string meshName;
		public string TextureFilepath { get; private set; }
		private string meshPath;

		//public to allow ref access
		public Vector3[] vertices = new Vector3[0];
		public Vector3[] normals = new Vector3[0];
		public Vector2[] uv = new Vector2[0];
		public int[] triangles = new int[0];



		private List<string> meshFileNameList = new List<string>();

		public MeshImageTile(string meshName, string textureFilename, string meshPath)
		{
			this.meshName = meshName;
			this.meshPath = meshPath;
			TextureFilepath = textureFilename;
		}

		private void AddDataToArray(ref Vector3[] array, ref Vector3[] dataToAdd)
		{
			int totalLength = array.Length + dataToAdd.Length;
			Vector3[] temp = new Vector3[totalLength];
			Array.Copy(array, 0, temp, 0, array.Length);
			Array.Copy(dataToAdd, 0, temp, array.Length, dataToAdd.Length);

			array = temp;
		}

		private void AddDataToArray(ref Vector2[] array, ref Vector2[] dataToAdd)
		{
			int totalLength = array.Length + dataToAdd.Length;
			Vector2[] temp = new Vector2[totalLength];
			Array.Copy(array, 0, temp, 0, array.Length);
			Array.Copy(dataToAdd, 0, temp, array.Length, dataToAdd.Length);

			array = temp;
		}

		private void AddDataToArray(ref int[] array, ref int[] dataToAdd)
		{
			int totalLength = array.Length + dataToAdd.Length;
			int[] temp = new int[totalLength];
			Array.Copy(array, 0, temp, 0, array.Length);
			Array.Copy(dataToAdd, 0, temp, array.Length, dataToAdd.Length);

			array = temp;
		}

		public void AddData(ref Vector3[] vertexArray, ref Vector3[] normalArray, ref Vector2[] uvArray, ref int[] triangleArray)
		{
			if (vertexArray.Length != normalArray.Length)
			{
				throw new ArgumentException("vertexArray.Length != normalArray.Length");
			}

			if (vertexArray.Length != uvArray.Length)
			{
				throw new ArgumentException("vertexArray.Length != normalArray.Length");
			}

			//triangles are indices of the vertex array.  Since we're going to append all these arrays,
			//I need to adjust the triangle indices to point to the appended vertex indices
			int[] adjustedTriangleArray = new int[triangleArray.Length];
			int currentNumVertices = vertices.Length;
			for(int i = 0; i < triangleArray.Length; i++)
			{
				adjustedTriangleArray[i] = triangleArray[i] + currentNumVertices;
			}
			AddDataToArray(ref vertices, ref vertexArray);
			AddDataToArray(ref normals, ref normalArray);
			AddDataToArray(ref uv, ref uvArray);
			AddDataToArray(ref triangles, ref adjustedTriangleArray);


			Logger.Log($"AddData num verts {vertices.Length}, max triangle index {triangles.Max()}");
		}


		public string[] GetMeshFileNames()
		{
			return meshFileNameList.ToArray();
		}
	}
}
