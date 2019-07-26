using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace MiplMeshToObj
{
	class Converter
	{

		public struct InputInfo
		{
			public readonly string inputMeshPath;
			public readonly string outputDirectory;

			public InputInfo(string inputMeshPath, string outputDirectory)
			{
				this.inputMeshPath = inputMeshPath;
				this.outputDirectory = outputDirectory;
			}
		}

		private Configuration configuration;

		enum TriangleMode { TRIANGLE_STRIP, FREE_VERTS, SINGLE_QUAD };

		//if the normals look like they are pointing inwards in the resulting mesh, try toggling this value.
		private const bool ivOsgxFlipOrder = true;
		private const bool pfbOsgxFlipOrder = false;

		
		public struct MeshConversionResult
		{
			public readonly bool success;
			public readonly string outputMeshPath;

			public MeshConversionResult(bool success, string outputMeshPath)
			{
				this.success = success;
				this.outputMeshPath = outputMeshPath;
			}

			public static readonly MeshConversionResult fail = new MeshConversionResult(false, "");
		}

		private struct Triangle
		{
			public readonly int v1;
			public readonly int v2;
			public readonly int v3;

			public bool IsDegenerate
			{
				get { return v1 == v2 || v2 == v3 || v1 == v3; }
			}

			public Triangle(int v1, int v2, int v3)
			{
				this.v1 = v1;
				this.v2 = v2;
				this.v3 = v3;
			}

			public override int GetHashCode()
			{
				return v1 * v2 * v3;
			}
		}


		public Converter(Configuration configuration)
		{
			this.configuration = configuration ?? throw new NullReferenceException("configuration is null");

		}




		private async Task<MeshConversionResult> ConvertToOsgxAsync(string inputMeshPath, string outputDirectory, CancellationToken cancellationToken)
		{

			if (cancellationToken.IsCancellationRequested)
				return MeshConversionResult.fail;

			string meshBaseName = Path.GetFileNameWithoutExtension(inputMeshPath);
			string osgxFilePath = Path.Combine(outputDirectory, meshBaseName + ".osgx");

			string command = configuration.PfbToOsgx;
			string args = $"{inputMeshPath} {osgxFilePath}";
			var result = await RunExternalProcess.RunAsync(command, args, cancellationToken, printStdout: true, printStderr: true).ConfigureAwait(false);
			if (!result.success)
				return MeshConversionResult.fail;

			//Make sure everything went smoothly
			if (!File.Exists(osgxFilePath))
			{
				throw new FileNotFoundException(osgxFilePath, $"Error:  No osgx file exists after attempted conversion to obj.  Command run: {command} {args}");
			}

			Logger.Log("ConvertToOsgxAsync() Done.");
			Logger.Log($"Memory: { (GC.GetTotalMemory(true) / 1024) } KB");


			return new MeshConversionResult(true, osgxFilePath);
		}

		



		public async Task<bool> ProcessMeshAsync(InputInfo inputInfo, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
				return false;

			Logger.Log($"ProcessMeshAsync(): Beginning {inputInfo.inputMeshPath}");
			Logger.Log($"Memory: { (GC.GetTotalMemory(true) / 1024) } KB");

			if (!Directory.Exists(inputInfo.outputDirectory))
			{
				Directory.CreateDirectory(inputInfo.outputDirectory);
			}

			
			var osgxResult = await ConvertToOsgxAsync(inputInfo.inputMeshPath, inputInfo.outputDirectory, cancellationToken).ConfigureAwait(false);
			if (!osgxResult.success)
				return false;

			if (cancellationToken.IsCancellationRequested)
			{
				Logger.Log("ProcessDataProducts(): Cancellation detected.  Throwing.");
				throw (new OperationCanceledException());
			}

			//rgb files live next to the mesh on the /oss and /ods, and usually by convention meshes and their textures are in the same directory
			string rgbDirectory = Path.GetDirectoryName(inputInfo.inputMeshPath);

			var objResult = await ConvertOsgxToObjAsync(osgxResult.outputMeshPath, rgbDirectory, inputInfo.outputDirectory, cancellationToken).ConfigureAwait(false);
			if (!objResult.success)
				return false;
			
			return true;
		}

		private const string childrenElementField = "Children";
		private const string osgGroupField = "osg--Group";
		private const string osgMatrixTransformField = "osg--MatrixTransform";
		private const string osgLodField = "osg--LOD";
		private const string osgGeometryField = "osg--Geometry";
		private const string nameElementField = "Name";
		private const string attributeAttributeField = "attribute";

		private class OsgGeometrySections
		{
			public bool Success { get; private set; } = true;
			public readonly Dictionary<string, List<XElement>> geometryDict = new Dictionary<string, List<XElement>>();

			public static readonly OsgGeometrySections fail = new OsgGeometrySections() { Success = false };
		}


		private IEnumerable<XElement> GetNextOsgGroups(XElement currentOsgGroup)
		{

			XElement childrenElement = currentOsgGroup.Element(childrenElementField);
			if (childrenElement == null)
			{
				Logger.Error("No Children element exists in osg--Group: {0}", currentOsgGroup.ToString());
				return new XElement[] { };
			}

			//skip matrix elements and LOD elements
			List<XElement> osgGroups = new List<XElement>();
			var testOsgMatrix = childrenElement.Elements(osgMatrixTransformField);
			if (testOsgMatrix != null && testOsgMatrix.Count() > 0)
			{
				Logger.Log("Found {0} osgMatrixTransform elements", testOsgMatrix.Count());
				//found a osg--MatrixTransform layer between osg--Groups.  
				foreach (var osgMatrix in testOsgMatrix)
				{
					osgGroups.AddRange(GetNextOsgGroups(osgMatrix));
				}
			}

			//MSL: There are no osg--LOD elements.  MER: there are
			var testOsgLod = childrenElement.Elements(osgLodField);
			if (testOsgLod != null && testOsgLod.Count() > 0)
			{
				Logger.Log("Found {0} osgLOD elements", testOsgLod.Count());
				//found a osg--LOD layer between osg--Groups.  
				foreach (var lod in testOsgLod)
				{
					osgGroups.AddRange(GetNextOsgGroups(lod));
				}
			}


			var testOsgGroup = childrenElement.Elements(osgGroupField);
			if (testOsgGroup != null && testOsgGroup.Count() > 0)
			{
				Logger.Log("Found {0} osgGroup elements", testOsgGroup.Count());
				osgGroups.AddRange(testOsgGroup);
				//found a osg--LOD layer between osg--Groups.  
				//foreach (var g in testOsgGroup)
				//{
				//	osgGroups.AddRange(GetNextOsgGroups(g));
				//}
			}

			return osgGroups;

			//if (testOsgGroup == null || testOsgGroup.Count() == 0)
			//{
			//	List<XElement> osgGroups = new List<XElement>();
			//	var testOsgMatrix = childrenElement.Elements(osgMatrixTransformField);
			//	if (testOsgMatrix != null && testOsgMatrix.Count() > 0)
			//	{
			//		Logger.Log("Found {0} osgMatrixTransform elements", testOsgMatrix.Count());
			//		//found a osg--MatrixTransform layer between osg--Groups.  
			//		foreach(var osgMatrix in testOsgMatrix)
			//		{
			//			osgGroups.AddRange(GetNextOsgGroups(osgMatrix));
			//		}
			//	}

			//	//MSL: There are no osg--LOD elements.  MER: there are
			//	var testOsgLod = childrenElement.Elements(osgLodField);
			//	if (testOsgLod != null && testOsgLod.Count() > 0)
			//	{
			//		Logger.Log("Found {0} osgLOD elements", testOsgLod.Count());
			//		//found a osg--LOD layer between osg--Groups.  
			//		foreach (var lod in testOsgLod)
			//		{
			//			osgGroups.AddRange(GetNextOsgGroups(lod));
			//		}
			//	}

			//	return osgGroups;
			//}
			//else if (testOsgGroup != null)
			//{
			//	return testOsgGroup;
			//}

			//return new XElement[] { };
		}

		private bool HasGeometryDescendants(XElement osgGroup)
		{
			return osgGroup.Descendants(osgGeometryField).Count() > 0;
		}

		private void GetGeometryRecursive(XElement osgGroup, ref OsgGeometrySections osgGeometrySections)
		{
			if (!HasGeometryDescendants(osgGroup))
			{
				Logger.Log($"{osgGroup.Name.LocalName} has no geometry, skipping.");
				return;
			}

			Logger.Log($"Examining {osgGroup.Name.LocalName}");
			//see if we're at a texture level
			bool foundName = false;
			//only accept names from actual osg groups, not matrix transforms, lods, etc.
			if (osgGroup.Name.LocalName == osgGroupField)
			{
				XElement nameTest = osgGroup.Element(nameElementField);
				if (nameTest != null)
				{
					XAttribute attributeTest = nameTest.Attribute(attributeAttributeField);
					if (attributeTest != null)
					{
						foundName = true;
						string textureBasename = attributeTest.Value.Replace("&quot;", "").Replace("\"", "").TrimStart(new char[] { '_' });
						Logger.Log($"Found texture {textureBasename}");
						List<XElement> geometrySections;
						if (!osgGeometrySections.geometryDict.TryGetValue(textureBasename, out geometrySections))
						{
							geometrySections = new List<XElement>();
							osgGeometrySections.geometryDict.Add(textureBasename, geometrySections);
						}
						geometrySections.AddRange(osgGroup.Descendants(osgGeometryField));
					}
					else
					{
						Logger.Log("attribute test is null");
					}
				}
				else
				{
					Logger.Log("name test is null");
				}
			}
			else
			{
				Logger.Log($"group is not {osgGroupField}");
			}

			//recursive branch
			if (!foundName)
			{
				Logger.Log($"Found no name in {osgGroup.Name.LocalName}, looking deeper");
				foreach(var g in GetNextOsgGroups(osgGroup))
				{
					GetGeometryRecursive(g, ref osgGeometrySections);
				}
			}
		}

		private OsgGeometrySections GetOsgGeometrySectionsLookingForExternalTextures(XElement root)
		{
			XElement matrixTransformElement = root.Element(osgMatrixTransformField);
			if (matrixTransformElement == null)
			{
				Logger.Error("matrixTransformElement is null");
				return OsgGeometrySections.fail;
			}

			OsgGeometrySections osgGeometrySections = new OsgGeometrySections();

			GetGeometryRecursive(matrixTransformElement, ref osgGeometrySections);

			return osgGeometrySections;
		}

		private OsgGeometrySections GetOsgGeometrySectionsLookingForInternalTextures(XElement root)
		{
			OsgGeometrySections osgGeometrySections = new OsgGeometrySections();

			foreach(var geometry in root.Descendants(osgGeometryField))
			{
				var textureList = geometry.Descendants("osg--Texture2D");
				int textureCount = textureList.Count();
				if (textureCount == 0)
				{
					Logger.Error("No texture in this geometry section.");
					continue;
				}
				if (textureCount > 1)
				{
					Logger.Error("This geometry section has more than one texture.");
					continue;
				}

				if (textureList.First().Element("Image") != null 
					&& textureList.First().Element("Image").Element("FileName") != null)
				{
					XAttribute attributeTest = textureList.First().Element("Image").Element("FileName").Attribute(attributeAttributeField);
					string textureBasename = attributeTest.Value.Replace("&quot;", "").Replace("\"", "").TrimStart(new char[] { '_' });
					textureBasename = Path.GetFileNameWithoutExtension(textureBasename);
					Logger.Log($"Found texture {textureBasename}");
					List<XElement> geometrySections;
					if (!osgGeometrySections.geometryDict.TryGetValue(textureBasename, out geometrySections))
					{
						geometrySections = new List<XElement>();
						osgGeometrySections.geometryDict.Add(textureBasename, geometrySections);
					}
					geometrySections.Add(geometry);
				}
					
			}

			return osgGeometrySections;
		}


		private async Task<MeshConversionResult> ConvertOsgxToObjAsync(
			string inputOsgxPath, 
			string inputRgbDirectory, 
			string outputDirectory, 
			CancellationToken cancellationToken)
		{
		
			if (cancellationToken.IsCancellationRequested)
			{
				Logger.Log("Cancellation requested.");
				return MeshConversionResult.fail;
			}

			Logger.Log("ConvertOsgxToObjAsync():  Beginning " + inputOsgxPath);

			//And also stop processing if we can't find our osgx file.
			if (!File.Exists(inputOsgxPath))
			{
				Logger.Error("The osgx file couldn't be found: " + inputOsgxPath);
				return MeshConversionResult.fail;
			}

			//if the directory to place processed meshes in doesn't exist, create it
			if (!Directory.Exists(outputDirectory))
			{
				Directory.CreateDirectory(outputDirectory);
			}

			string osgxDir = Path.GetDirectoryName(inputOsgxPath);

			//scan the osgx file for materials first, so I can construct atlases
			try
			{

				
				XElement root = XElement.Load(inputOsgxPath);

				//Look for texture references outside of geometry first, this happens in cases other than MER HiRise.
				OsgGeometrySections osgGeometrySections = GetOsgGeometrySectionsLookingForExternalTextures(root);

				if (!osgGeometrySections.Success || osgGeometrySections.geometryDict.Count() == 0)
				{
					//Look for texture references inside of geometry
					osgGeometrySections = GetOsgGeometrySectionsLookingForInternalTextures(root);

					if (!osgGeometrySections.Success)
					{
						return MeshConversionResult.fail;
					}

				}

				List<string> textureBasenames = osgGeometrySections.geometryDict.Keys.ToList();

				Logger.Log("Found {0} texture names", textureBasenames.Count);


				var result = await ConvertRgbFilesAsync(
					textureBasenames.ToArray(),
					textureDirectory: inputRgbDirectory,
					outputDirectory: outputDirectory,
					cancellationToken: cancellationToken)
					.ConfigureAwait(false);


				if (!result.success)
				{
					Logger.Error("Unsuccessful processing of textures, returning false.");
					return MeshConversionResult.fail;
				}

				string osgxName = Path.GetFileNameWithoutExtension(inputOsgxPath);
				
				Dictionary<string, MeshImageTile> textureBasenameToMeshSectionDict = new Dictionary<string, MeshImageTile>();
				var meshImageTiles = new List<MeshImageTile>();
				foreach (string basename in textureBasenames)
				{
					
					MeshImageTile meshImageTile = new MeshImageTile(osgxName, result.textureBasenameToPathDict[basename], outputDirectory);
					meshImageTiles.Add(meshImageTile);
					textureBasenameToMeshSectionDict.Add(basename, meshImageTile);
				}


				Logger.Log("Initialized mesh image tiles");

				
				foreach (string textureBasename in osgGeometrySections.geometryDict.Keys)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						Logger.Log("Cancellation requested.");
						return MeshConversionResult.fail;
					}

					foreach (XElement geometry in osgGeometrySections.geometryDict[textureBasename])
					{

						if (cancellationToken.IsCancellationRequested)
						{
							Logger.Log("Cancellation requested.");
							return MeshConversionResult.fail;
						}

						Vector3[] vertices = null;
						Vector3[] normals = null;
						Vector2[] uvs = null;
						int[] triangles = null;


						//MER pfb's have triangle strips.
						//MSL iv's have free verts
						//MER HiRise have single quads
						TriangleMode triangleMode = TriangleMode.FREE_VERTS;
						int triangleStripCount = 0;
						int[] triangleStripVertexCountArray = new int[] { };
						if (geometry.Element("PrimitiveSetList") != null && geometry.Element("PrimitiveSetList").Element("DrawArraysLength") != null)
						{
							triangleMode = TriangleMode.TRIANGLE_STRIP;

							//Old Openscenegraph OSGX spec
							string[] triangleStripStrvec =
								geometry
								.Element("PrimitiveSetList")
								.Element("DrawArraysLength")
								.Attribute("text")
								.Value
								.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
							//Logger.Log("triangle strip count {0}", triangleStripStrvec.Length);

							triangleStripCount = triangleStripStrvec.Length;
							triangleStripVertexCountArray = new int[triangleStripCount];
							for (int i = 0; i < triangleStripCount; i++)
							{
								triangleStripVertexCountArray[i] = Convert.ToInt32(triangleStripStrvec[i]);
							}

						}
						else if (geometry.Element("PrimitiveSetList") != null && geometry.Element("PrimitiveSetList").Element("osg--DrawArrayLengths") != null)
						{
							triangleMode = TriangleMode.TRIANGLE_STRIP;

							//newer openscenegraph OSGX spec
							string[] triangleStripStrvec =
								geometry
								.Element("PrimitiveSetList")
								.Element("osg--DrawArrayLengths")
								.Element("vector")
								.Attribute("text")
								.Value
								.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
							//Logger.Log("triangle strip count {0}", triangleStripStrvec.Length);

							triangleStripCount = triangleStripStrvec.Length;
							triangleStripVertexCountArray = new int[triangleStripCount];
							for (int i = 0; i < triangleStripCount; i++)
							{
								triangleStripVertexCountArray[i] = Convert.ToInt32(triangleStripStrvec[i]);
							}

						}
						else if (geometry.Element("PrimitiveSetList") != null
							&& geometry.Element("PrimitiveSetList").Element("osg--DrawArrays") != null
							&& geometry.Element("PrimitiveSetList").Element("osg--DrawArrays").Element("Mode") != null
							&& geometry.Element("PrimitiveSetList").Element("osg--DrawArrays").Element("Mode").Attribute(attributeAttributeField).Value == "QUADS")
						{
							//This happens in the case where we have a single quad for a MER HiRIse mesh
							triangleMode = TriangleMode.SINGLE_QUAD;
						}
						else
						{
							triangleMode = TriangleMode.FREE_VERTS;
						}




						Logger.Log("getting vertex data");


						//vertex data

						//There appears to be different kinds of osgx files.  Some have VertexData and others VertexArray, etc.
						XAttribute vertexTextAttribute = null;
						if (geometry.Element("VertexData") != null
							&& geometry.Element("VertexData").Element("Array") != null
							&& geometry.Element("VertexData").Element("Array").Element("ArrayID") != null)
						{
							vertexTextAttribute = geometry.Element("VertexData").Element("Array").Element("ArrayID").Attribute("text");
						}
						else if (geometry.Element("VertexArray") != null
							&& geometry.Element("VertexArray").Element("osg--Vec3Array") != null
							&& geometry.Element("VertexArray").Element("osg--Vec3Array").Element("vector") != null)
						{
							vertexTextAttribute = geometry.Element("VertexArray").Element("osg--Vec3Array").Element("vector").Attribute("text");
						}

						if (vertexTextAttribute == null)
						{
							//no data here, continue
							Logger.Log("Couldn't find any vertices in this geometry section");
							continue;
						}
						string[] vertexStrvec = vertexTextAttribute.Value.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
						if (vertexStrvec.Length % 3 != 0)
						{
							Logger.Error("Vertex data strvec length is not a multiple of 3");
						}
						Logger.Log("vertexStrvec first element is {0}, length is {1}", vertexStrvec[0], vertexStrvec.Length);
						int numVertices = vertexStrvec.Length / 3;
						if (triangleMode == TriangleMode.SINGLE_QUAD && numVertices != 4)
						{
							Logger.Error("This form of osgx file is unsupported.  Found QUAD type, and there are more than four vertices.  " +
								"So far, this only handles single quads for MER HiRIse meshes.");
							return MeshConversionResult.fail;
						}
						Vector3[] vertexArray = new Vector3[numVertices];

						List<Vector3> verticesList = new List<Vector3>();
						List<int> trianglesList = new List<int>();
						//Dictionary<double, int> indexCacheDict = new Dictionary<double, int>();
						for (int i = 0; i < vertexStrvec.Length; i += 3)
						{
							if (cancellationToken.IsCancellationRequested)
							{
								Logger.Log("Cancellation requested.");
								return MeshConversionResult.fail;
							}

							float c1 = Convert.ToSingle(vertexStrvec[i]);
							float c2 = Convert.ToSingle(vertexStrvec[i + 1]);
							float c3 = Convert.ToSingle(vertexStrvec[i + 2]);

							vertexArray[i / 3] = new Vector3(c1, c2, c3);

							verticesList.Add(vertexArray[i / 3]);
							trianglesList.Add(verticesList.Count() - 1);

						}

						Logger.Log("Done parsing vertices.");

						//It looks like all LODs are at the same depth and have different centers.  So not sure if there are actually
						//different LODs or how to recognize them is they exist.
						//Logger.Log(
						//	$"{textureBasename}: lods? {geometry.Ancestors().Where(x => x.Name.LocalName == osgLodField).Count() > 0} " +
						//	$"depth {geometry.Ancestors().Count()} num verts {verticesList.Count}");

						switch (triangleMode)
						{
							case TriangleMode.FREE_VERTS:
								{
									Logger.Log("Making triangles from vertices, not strips.");
									//see if we have triangles listed more than once, and if so, remove duplicates
									//also remove degenerates (triangles with 2 of the same vertex)
									List<Triangle> uniqueTriangleStructList = new List<Triangle>();
									for (int i = 0; i < trianglesList.Count - 2; i += 3)
									{
										Triangle t = new Triangle(trianglesList[i], trianglesList[i + 1], trianglesList[i + 2]);

										uniqueTriangleStructList.Add(t);
									}


									//int[] trianglesArray;
									int maxTriangle = 0;
									if (uniqueTriangleStructList.Count * 3 < trianglesList.Count || ivOsgxFlipOrder)
									{
										int duplicateCount = trianglesList.Count / 3 - uniqueTriangleStructList.Count;
										if (duplicateCount > 0)
											Logger.Log("Found {0} duplicate and/or degenerate triangles", duplicateCount);

										triangles = new int[uniqueTriangleStructList.Count * 3];
										int i = 0;
										foreach (Triangle t in uniqueTriangleStructList)
										{
											if (ivOsgxFlipOrder)
											{
												triangles[i++] = t.v1;
												triangles[i++] = t.v3;
												triangles[i++] = t.v2;
											}
											else
											{
												triangles[i++] = t.v1;
												triangles[i++] = t.v2;
												triangles[i++] = t.v3;
											}

											maxTriangle = maxTriangle < t.v1 ? t.v1 : maxTriangle;
											maxTriangle = maxTriangle < t.v2 ? t.v2 : maxTriangle;
											maxTriangle = maxTriangle < t.v3 ? t.v3 : maxTriangle;
										}

									}
									else
									{
										triangles = trianglesList.ToArray();
									}
									uniqueTriangleStructList = null;




									if (vertexArray.Length % 3 != 0)
									{
										Logger.Error($"There are not a multiple of 3 number of vertices listed in the osgx file, vertexArray.Length is {vertexArray.Length}. How should I make triangles?");
									}

									if (maxTriangle > verticesList.Count())
									{
										Logger.Error("triangles max vertex index {0}, number of vertices {1}", maxTriangle, verticesList.Count());
									}


									Logger.Log("Vertices parsed.  triangle  max vertex index {0}, number of vertices {1}", maxTriangle, verticesList.Count());
									break;
								}
							case TriangleMode.TRIANGLE_STRIP:
								{
									Logger.Log("Making triangles from strips.");
									//Now make triangles
									//construct triangles from triangle strip info
									//not the usual strip definition...
									//apparently each strip int value is a strip in itself. so a sequence of 3 3 3, etc means to take each successive
									//triplet of the vertex array, and contruct a triangle out of it.  4 means do the same, but with the fourth
									//vertex, use the previous 3 and 2 to make a second triangle.
									// numtri = numstrips + (numverts - numstrips*3)  = numverts - 2*numStrips;
									//reasoning:  first triangle uses 3 vertices.  all subsequence triangles use 1 additional vertex.
									//so numStrips gives the first triangle, which has 3 vertices.  The remaining number of vertices
									//all account for an additional triangle
									int numTriangles = 3 * (numVertices - 2 * triangleStripCount);
									triangles = new int[numTriangles];
									int vIndex = 0;
									int triIndex = 0;
									try
									{
										foreach (int ts in triangleStripVertexCountArray)
										{
											for (int vi = 0; vi < ts - 2; vi++)
											{
												if (pfbOsgxFlipOrder)
												{
													//if odd triangle
													if (vi % 2 > 0)
													{
														triangles[triIndex++] = vIndex + vi;
														triangles[triIndex++] = vIndex + vi + 2;
														triangles[triIndex++] = vIndex + vi + 1;
													}
													else
													{
														//even triangle, reverse first two indices
														triangles[triIndex++] = vIndex + vi + 1;
														triangles[triIndex++] = vIndex + vi + 2;
														triangles[triIndex++] = vIndex + vi;
													}
												}
												else
												{
													//if odd triangle
													if (vi % 2 > 0)
													{
														triangles[triIndex++] = vIndex + vi;
														triangles[triIndex++] = vIndex + vi + 1;
														triangles[triIndex++] = vIndex + vi + 2;
													}
													else
													{
														//even triangle, reverse first two indices
														triangles[triIndex++] = vIndex + vi + 1;
														triangles[triIndex++] = vIndex + vi;
														triangles[triIndex++] = vIndex + vi + 2;
													}
												}

											}
											vIndex += ts;
										}
									}
									catch (Exception e)
									{
										Logger.Error("Caught exception: {0}", e);
									}
									if (numTriangles != triIndex)
										Logger.Error("predicted num triangles {0}, actual num triangles {1}", numTriangles, triIndex);

									break;
								}
							case TriangleMode.SINGLE_QUAD:
								{
									triangles = new int[6];
									triangles[0] = 1;
									triangles[1] = 0;
									triangles[2] = 2;
									triangles[3] = 2;
									triangles[4] = 0;
									triangles[5] = 3;
									break;
								}
						}

						if (cancellationToken.IsCancellationRequested)
						{
							Logger.Log("Cancellation requested.");
							return MeshConversionResult.fail;
						}

						//normals

						switch (triangleMode)
						{
							case TriangleMode.FREE_VERTS:
							case TriangleMode.TRIANGLE_STRIP:
								{
									string[] normalStrvec = new string[] { };
									if (geometry.Element("NormalData") != null)
									{
										normalStrvec =
											geometry
											.Element("NormalData")
											.Element("Array")
											.Element("ArrayID")
											.Attribute("text")
											.Value
											.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
									}
									else if (geometry.Element("NormalArray") != null)
									{
										normalStrvec =
											geometry
											.Element("NormalArray")
											.Element("osg--Vec3Array")
											.Element("vector")
											.Attribute("text")
											.Value
											.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
									}
									else
									{
										Logger.Error("Couldn't find NormalData or NormalArray section.");
									}

									if (normalStrvec.Length % 3 != 0)
									{
										Logger.Error("Normal data strvec length is not a multiple of 3");
									}
									else if (normalStrvec.Length != vertexStrvec.Length)
									{
										if (normalStrvec.Length == 3)
										{

										}

										Logger.Error("normalStrvec.Length {0} is not equal to vertexStrvec.Length {1}", normalStrvec.Length, vertexStrvec.Length);
									}


									normals = new Vector3[numVertices];
									for (int i = 0; i < vertexStrvec.Length; i += 3)
									{

										//sometimes if there is a single triangle, we'll have one normal for all three verts.
										//if a single normal, I'll assign to all verts.
										if (i < normalStrvec.Length)
										{
											float c1 = Convert.ToSingle(normalStrvec[i]);
											float c2 = Convert.ToSingle(normalStrvec[i + 1]);
											float c3 = Convert.ToSingle(normalStrvec[i + 2]);
											normals[i / 3] = new Vector3(c1, c2, c3);//.SaeToUnityCoordinateSystem();
										}
										else if (normalStrvec.Length >= 3 && i > 2)
										{
											normals[i / 3] = normals[0];
										}
									}

									break;
								}
							case TriangleMode.SINGLE_QUAD:
								{
									normals = new Vector3[4];
									for (int i = 0; i < normals.Length; i++)
									{
										normals[i] = new Vector3(0, 0, -1);
									}
									break;
								}
						}
						Logger.Log("Normals parsed.");


						if (cancellationToken.IsCancellationRequested)
						{
							Logger.Log("Cancellation requested.");
							return MeshConversionResult.fail;
						}

						//uv
						switch (triangleMode)
						{
							case TriangleMode.FREE_VERTS:
							case TriangleMode.TRIANGLE_STRIP:
								{
									string[] textureStrvec = new string[] { };
									if (geometry.Element("TexCoordData") != null)
									{
										textureStrvec =
											geometry
											.Element("TexCoordData")
											.Element("Data")
											.Element("Array")
											.Element("ArrayID")
											.Attribute("text")
											.Value
											.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
									}
									else if (geometry.Element("TexCoordArrayList") != null)
									{
										textureStrvec =
											geometry
											.Element("TexCoordArrayList")
											.Element("osg--Vec2Array")
											.Element("vector")
											.Attribute("text")
											.Value
											.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
									}
									else
									{
										Logger.Error("Couldn't find TexCoordData or TexCoordArrayList section.");
									}

									if (textureStrvec.Length % 2 != 0)
									{
										Logger.Error("Texture uv data strvec length is not a multiple of 2");
									}
									if (textureStrvec.Length / 2 != numVertices)
									{
										Logger.Error("Texture uv datalength is not the same as num vertices. texCoords: {0}, vertices {1}",
											textureStrvec.Length / 2, numVertices);
									}
									uvs = new Vector2[textureStrvec.Length / 2];
									for (int i = 0; i < textureStrvec.Length; i += 2)
									{

										float c1 = Convert.ToSingle(textureStrvec[i]);
										float c2 = Convert.ToSingle(textureStrvec[i + 1]);
										uvs[i / 2] = new Vector2(c1, c2);
									}

									
									break;
								}
							case TriangleMode.SINGLE_QUAD:
								{
									uvs = new Vector2[4];
									uvs[0] = new Vector2(1, 0);
									uvs[1] = new Vector2(1, 1);
									uvs[2] = new Vector2(0, 1);
									uvs[3] = new Vector2(0, 0);
									break;
								}
						}

						Logger.Log("uv parsed.");

						vertices = verticesList.ToArray();

						//Logger.Log("Initialized Geometry section. currentNumVertices {0}, unique vertices {1}", numVertices, verticesList.Count);


						if (cancellationToken.IsCancellationRequested)
						{
							Logger.Log("Cancellation requested.");
							return MeshConversionResult.fail;
						}

						//Now that we've parsed everything we need for this geometry section, add it to the MeshImageTile for this texture.
						MeshImageTile meshImageTile;
						if (textureBasenameToMeshSectionDict.TryGetValue(textureBasename, out meshImageTile))
						{
							meshImageTile.AddData(ref vertices, ref normals, ref uvs, ref triangles);
						}
						else
						{
							Logger.Error("MeshSection doesn't exist in dictionary for textureBasename {0}", textureBasename);
						}
					}
				}
				

				string objFilename = Path.GetFileNameWithoutExtension(inputOsgxPath) + ".obj";
				string mtlFilename = Path.GetFileNameWithoutExtension(inputOsgxPath) + ".mtl";
				string objPath = Path.Combine(outputDirectory, objFilename);
				string mtlPath = Path.Combine(outputDirectory, mtlFilename);
				WriteObj(meshImageTiles, objPath, mtlPath);

				Logger.Log($"Wrote: {Path.GetFileName(objPath)}");
				return new MeshConversionResult(true, objPath);

			}
			catch (Exception e)
			{

				Logger.Error("Caught exception: {0}", e);
				return MeshConversionResult.fail;
			}

		}


		private void WriteObj(List<MeshImageTile> meshImageTiles, string objPath, string mtlPath)
		{
			Vector3[] vertices = new Vector3[0];
			Vector3[] normals = new Vector3[0];
			Vector2[] uv = new Vector2[0];
			int[] faces = new int[0];
			string[] materialNames = new string[meshImageTiles.Count];

			using (var objStream = new StreamWriter(objPath))
			{
				using (var mtlStream = new StreamWriter(mtlPath))
				{
					objStream.WriteLine($"mtllib {Path.GetFileName(mtlPath)}");
					objStream.WriteLine("");
					objStream.WriteLine("# vertices");
					for (int i = 0; i < meshImageTiles.Count; i++)
					{
						for (int v = 0; v < meshImageTiles[i].vertices.Length; v++)
						{
							objStream.WriteLine($"v {meshImageTiles[i].vertices[v].x} {meshImageTiles[i].vertices[v].y} {meshImageTiles[i].vertices[v].z}");
						}
					}
					objStream.WriteLine("");
					objStream.WriteLine("# normals");
					for (int i = 0; i < meshImageTiles.Count; i++)
					{
						for (int n = 0; n < meshImageTiles[i].normals.Length; n++)
						{
							objStream.WriteLine($"vn {meshImageTiles[i].normals[n].x} {meshImageTiles[i].normals[n].y} {meshImageTiles[i].normals[n].z}");
						}
					}

					objStream.WriteLine("");
					objStream.WriteLine("# uv");
					for (int i = 0; i < meshImageTiles.Count; i++)
					{
						for (int u = 0; u < meshImageTiles[i].uv.Length; u++)
						{
							objStream.WriteLine($"vt {meshImageTiles[i].uv[u].x} {meshImageTiles[i].uv[u].y}");
						}
					}

					objStream.WriteLine("");
					objStream.WriteLine("# faces");
					//obj face indices start at 1, not 0.
					int faceIndexOffset = 1;
					for (int i = 0; i < meshImageTiles.Count; i++)
					{
						string materialName = $"material_{i}";
						string textureName = Path.GetFileName(meshImageTiles[i].TextureFilepath);

						mtlStream.WriteLine($"newmtl {materialName}");
						mtlStream.WriteLine("Ka 1.000000 1.000000 1.000000");
						mtlStream.WriteLine("Kd 1.000000 1.000000 1.000000");
						mtlStream.WriteLine("Ks 0.000000 0.000000 0.000000");
						mtlStream.WriteLine("Tr 1.000000");
						mtlStream.WriteLine("illum 1");
						mtlStream.WriteLine("Ns 0.000000");
						mtlStream.WriteLine($"map_Kd {textureName}");
						mtlStream.WriteLine($"");

						objStream.WriteLine("");
						objStream.WriteLine($"usemtl {materialName}");

						for (int f = 0; f < meshImageTiles[i].triangles.Length; f = f + 3)
						{
							int f1 = meshImageTiles[i].triangles[f] + faceIndexOffset;
							int f2 = meshImageTiles[i].triangles[f + 1] + faceIndexOffset;
							int f3 = meshImageTiles[i].triangles[f + 2] + faceIndexOffset;
							objStream.WriteLine($"f {f1}/{f1}/{f1} {f2}/{f2}/{f2} {f3}/{f3}/{f3}");
						}

						faceIndexOffset += meshImageTiles[i].vertices.Length;
					}
				}
			}
		}


		internal struct ConvertRgbFilesResult
		{
			public readonly bool success;
			public readonly Dictionary<string, string> textureBasenameToPathDict;

			public ConvertRgbFilesResult(bool success, Dictionary<string, string> textureBasenameToPathDict)
			{
				this.success = success;
				this.textureBasenameToPathDict = textureBasenameToPathDict;
			}

			public static readonly ConvertRgbFilesResult fail = new ConvertRgbFilesResult(false, null);
		}

		private async Task<ConvertRgbFilesResult> ConvertRgbFilesAsync(
			string[] textureFileBasenames, 
			string textureDirectory, 
			string outputDirectory, 
			CancellationToken cancellationToken)
		{
			
			//First process the textures so that the mesh processing can make an atlas out of them.
			//Process .rgb textures to .jpg or .png, and place in processedDir
			try
			{
				Dictionary<string, string> textureBasenameToPathDict = new Dictionary<string, string>();

				//make sure our processedDir exists, create it if not
				if (!Directory.Exists(outputDirectory))
				{
					Directory.CreateDirectory(outputDirectory);
				}

				List<Task<bool>> taskList = new List<Task<bool>>();

				//occasionally an obsolete version of a texture file is referenced in a mesh, and that obsolete version
				//no longer exists.  Rather than checking for that case specifically, always find the highest version of
				//a texture file, process it, and name it what the mesh is expecting.
				foreach (string basename in textureFileBasenames)
				{
					//abort if cancelled
					if (cancellationToken.IsCancellationRequested)
					{
						return ConvertRgbFilesResult.fail;
					}

					Logger.Log("processing {0}", basename);
					//substring to take off version number.
					string unprocessedImagePattern = basename.Substring(0, basename.Length - 1) + "*.rgb";

					string[] unprocessedImageFiles = Directory.GetFiles(textureDirectory, unprocessedImagePattern, SearchOption.AllDirectories);
					if (unprocessedImageFiles != null && unprocessedImageFiles.Length > 0)
					{
						//if there's a different version, use the highest version available (sort and take the last)
						string unprocessedFile = unprocessedImageFiles.Length == 1 ? unprocessedImageFiles[0] : unprocessedImageFiles.OrderBy(x => x).Last();


						string processedFilePathAbsolute = Path.Combine(outputDirectory, basename + ".png");

						if (!File.Exists(processedFilePathAbsolute))
						{
							taskList.Add(RunConvertRgbProcessAsync(unprocessedFile, processedFilePathAbsolute, cancellationToken));
						}

						textureBasenameToPathDict[basename] = processedFilePathAbsolute;
					}
					else
					{
						//no texture file found for this texture
						Logger.Error("Texture file does not exist: dir: {0} pattern: {1}.  Cancelling.", textureDirectory, unprocessedImagePattern);
						return ConvertRgbFilesResult.fail;
					}

				}



				foreach (bool result in await Task.WhenAll(taskList).ConfigureAwait(false))
				{
					if (!result)
					{
						Logger.Error("Error processing rgb texture");
						return ConvertRgbFilesResult.fail;
					}
				}

				return new ConvertRgbFilesResult(true, textureBasenameToPathDict);
			}
			catch (Exception e)
			{
				Logger.Error("Caught Exception: {0}", e);
				return ConvertRgbFilesResult.fail;
			}
			
		}


		protected async Task<bool> RunConvertRgbProcessAsync(string rgbPath, string outputPath, CancellationToken cancellationToken)
		{
			string command = configuration.ConvertRgb;
			string arguments = rgbPath + " " + outputPath;
			var result = await RunExternalProcess.RunAsync(command, arguments, cancellationToken, printStdout: true, printStderr: true).ConfigureAwait(false);

			if (!result.success)
				return false;

			//check to make sure it worked
			if (!File.Exists(outputPath))
			{
				throw new FileNotFoundException(outputPath, $"Error:  No jpg file exists after attempted conversion from rgb.  Command run: {command} {arguments}");
			}

			return true;
		}

	}
}
