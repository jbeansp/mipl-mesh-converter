# mipl-mesh-converter
Converts MIPL/IDS generated .pfb and .iv meshes to .obj format.

This repository includes a Dockerfile that will compile a Docker image containing OpenSceneGraph with Performer plugin support.  This enables the use of the osgconv utility to convert .pfb and .iv meshes to other formats.  If you use osgconv to convert to .obj directly, the resulting meshes are pretty messy and won't load in some software.  So there is also C# code included that uses osgconv to convert to .osgx (OpenSceneGraph's xml format), parse out the vertices, normals, uv, and textures, and writes out a cleaner .obj file.

### Dependencies:
1. bash
2. python 2
3. Docker

### Usage:
1. Clone this repository:<br>
`git clone https://github.com/jbeansp/mipl-mesh-converter.git .`<br>
2. Run:<br>
    `mipl-mesh-converter/bin/compile-docker`<br>
    (This will take a long time, but only needs to be done once to compile the docker image)<br>
3. Convert a MER or MSL .pfb or .iv mesg to .obj by running:<br>
    `mipl-mesh-converter/bin/convert-mipl-mesh </path/to/mipl/mesh> </output/directory>`

### Notes:
* Texture images in .rgb format should be in the same directory as the input .pfb or .iv mesh.  The .rgb images will be converted to .png as part of the mesh conversion.
* The mesh coordinates are left in SAE (x = north, y = east, z = nadir/down).  The origin of the coordinate frame is defined by the Site frame the rover was in when the imagery for the mesh was taken.  (The Site and Drive of the rover's position is included in the .pfb or .iv mesh's filename, according to MIPL's EDR filename convention.)

### Some useful commands:
If it's running and you want to get inside the container for some reason:<br>
* `docker container ls`  
Then note the beginning of container hash.  Let's say it's 0d83jd98j3d.... for the following example commands.  Usually the first two characters of the hash are sufficient to identify it, so I'll use 0d below.<br>
* `docker exec -it 0d /bin/bash`  
This will give you a bash prompt inside the running container.<br>
* `docker kill 0d`  
Will kill the container.<br>
* `docker run -it --rm --entrypoint="" mipl-mesh-converter /bin/bash`  
Get a terminal inside a container that isn't processing a mesh.<br>
  
  
  

    
    
