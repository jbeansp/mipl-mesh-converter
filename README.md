# mipl-mesh-converter
Converts .pfb and .iv MIPL/IDS meshes to .obj format.

The docker image installs OpenSceneGraph with Performer plugin support.  This enables the use of the osgconv utility to convert .pfb meshes to other formats.  It also converts .iv meshes to .obj.  If you use osgconv to convert to .obj directly, the resulting meshes are pretty messy and won't load in some software.  So I convert to .osgx (OpenSceneGraph's xml format), parse out the vertices, normals, uv, and textures, and write out a cleaner .obj.

Dependencies:
1. bash
2. python 2
3. Docker

Usage:
1. Clone this repository:<br>
`git clone https://github.com/jbeansp/mipl-mesh-converter.git .`<br>
2. Run:<br>
    `mipl-mesh-converter/bin/compile-docker`<br>
    (this only needs to be done once to compile the docker image)<br>
3. Convert a MER or MSL .pfb or .iv mesg to .obj by running:<br>
    `mipl-mesh-converter/bin/convert-mipl-mesh </path/to/mipl/mesh> </output/directory>`
<br>
<br>
Useful commands:
  If it's running and you want to get inside the container for some reason:<br>
  `docker container ls`  (then note the beginning of container hash, say it's 0d83jd98j3d....)<br>
  `docker exec -it 0d /bin/bash`  (this will give you a bash prompt inside the running container)<br>
  `docker kill 0d`  (will kill the container)<br>
  `docker run -it `<br>
  
  
  

    
    
