# mipl-mesh-converter
Converts MIPL/IDS generated .pfb and .iv meshes to .obj format.

This repository includes a Dockerfile that will compile a Docker image containing [OpenSceneGraph](http://www.openscenegraph.org) with Performer plugin support.  This enables the use of the osgconv utility to convert .pfb and .iv meshes to other formats.  If you use osgconv to convert to .obj directly, the resulting meshes are pretty messy and won't load in some software.  So I instead use osgconv to convert to .osgx (OpenSceneGraph's xml format), parse out the vertices, normals, uv, and textures, and writes out a cleaner .obj file.

### Dependencies:
You need to have these installed on your machine first:
1. Docker
2. python 2
3. bash

### Usage:
1. Clone this repository:<br>
`git clone https://github.com/jbeansp/mipl-mesh-converter.git .`<br>
2. Compile the Docker image:<br>
    `mipl-mesh-converter/bin/compile-docker`<br>
    This only needs to be done once.<br>
3. Wait a long time for it to compile.  Maybe go get some coffee or something.<br>
4. It's ready to go!  Convert a MER or MSL .pfb or .iv mesh to .obj by running:<br>
    `mipl-mesh-converter/bin/convert-mipl-mesh /path/to/mipl/mesh /output/directory`

### Notes:
* Texture images in .rgb format should be in the same directory as the input .pfb or .iv mesh.  The .rgb images will be converted to .png as part of the mesh conversion.
* MER .pfb and .iv meshes have multiple level of details (LOD) within the mesh.  I'm not sure if the lower resolution LODs survive the conversion to .osgx.  The LOD levels in the .osgx file occur at the same xml tree depth per texture, and all have different x,y,z center values. So it's not clear to me if there are multiple LODs present, or how to separate the LODs if they do exist, so all vertices from all LOD sections are included in the final .obj file.  
* The mesh coordinates are left in SAE (x = north, y = east, z = nadir/down).  The origin of the coordinate frame is defined by the Site frame the rover was in when the imagery for the mesh was taken.  (The Site and Drive of the rover's position is included in the .pfb or .iv mesh's filename, according to MIPL's EDR filename convention.)

### Some useful commands for debugging:
* If the Docker container is running and you want to get inside it for some reason, you first need to know its id by running:<br>
`docker container ls`  
Note down the beginning of container hash.  Let's say it's 0d83jd98j3d.... for the following example commands.  Usually the first two characters of the hash are sufficient to identify it, so I'll use 0d below.<br>
* `docker exec -it 0d /bin/bash`  
This will give you a bash prompt inside the running container.<br>
* `docker kill 0d`  
Will kill the container.<br>
* `docker run -it --rm --entrypoint="" mipl-mesh-converter /bin/bash`  
Create a new container that isn't processing a mesh, and get a bash prompt inside it.<br>
  
  
  

    
    
