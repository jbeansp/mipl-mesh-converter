# mipl-mesh-converter
Converts MIPL/IDS generated .pfb and .iv meshes to .obj format.

The docker image installs OpenSceneGraph with Performer plugin support.  This enables the use of the osgconv utility to convert .pfb meshes to other formats.  It also converts .iv meshes to .obj.  If you use osgconv to convert to .obj directly, the resulting meshes are pretty messy and won't load in some software.  So I convert to .osgx (OpenSceneGraph's xml format), parse out the vertices, normals, uv, and textures, and write out a cleaner .obj.

### Dependencies:
1. bash
2. python 2
3. Docker

### Usage:
1. Clone this repository:<br>
`git clone https://github.com/jbeansp/mipl-mesh-converter.git .`<br>
2. Run:<br>
    `mipl-mesh-converter/bin/compile-docker`<br>
    (this only needs to be done once to compile the docker image)<br>
3. Convert a MER or MSL .pfb or .iv mesg to .obj by running:<br>
    `mipl-mesh-converter/bin/convert-mipl-mesh </path/to/mipl/mesh> </output/directory>`

### Notes:
The mesh coordinates are left in SAE (x = north, y = east, z = nadir/down).  The origin of the coordinate frame is defined by the Site frame the rover was at when the imagery for the mesh was taken.  MIPL includes the Site and Drive of the rover's position in the mesh's filename.

### Some useful commands:
If it's running and you want to get inside the container for some reason:<br>
* `docker container ls`  (then note the beginning of container hash, say it's 0d83jd98j3d....)<br>
* `docker exec -it 0d /bin/bash`  (this will give you a bash prompt inside the running container)<br>
* `docker kill 0d`  (will kill the container)<br>
* `docker run -it `<br>
  
  
  

    
    
