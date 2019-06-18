#!/usr/bin/python

import os
import sys
import argparse
import subprocess


def parseArgv():
	parser = argparse.ArgumentParser(description="Convert a MIPL .pfb or .iv mesh for MER or MSL to .obj format.  ", 
		epilog="E.g., convert-mipl-mesh.py --develop --updatetestserver --staging --merb --windows --overwrite-patcher " + \
		"-c -v auto -p previous current -o --run-on-osx-only -l 100")

	parser.add_argument("-m", "--mipl-mesh-path", metavar=('MIPL_MESH_PATH', required=True), 
		help="The path of the mipl mesh to convert.")

	parser.add_argument("-d", "--output-directory", metavar=('OUTPUT_DIR', required=True), 
		help="The directory to write the obj file to.")

	rovergroup = parser.add_mutually_exclusive_group(required=True)
	rovergroup.add_argument("--mer", action="store_true")
	rovergroup.add_argument("--msl", action="store_true")

	args = parser.parse_args()

	return args



if __name__ == '__main__':

	args = parseArgv()

	if args.mipl_mesh_path is not None:
		miplMeshPath = args.mipl_mesh_path[0]
	else:
		print("No mipl_mesh_path given")
		exit(1)

	if not os.path.isfile(miplMeshPath):
		print("file does not exist: %s" % miplMeshPath)
		exit(1)

	if args.output_directory is not None:
		outputDirectory = args.output_directory[0]
	else:
		print("No output_directory given")
		exit(1)	

	if args.mer:
		rover = "MER"
	elif args.msl:
		rover = "MSL"
	else:
		print("Rover not recognized")
		exit(1)

	if not os.path.exists(outputDirectory):
		os.mkdir(outputDirectory)

	miplMeshDirectory = os.path.dirname(miplMeshPath)
	miplMeshFilename = os.path.basename(miplMeshPath)

	dockerInputDirectory = "/input"
	dockerOutputDirectory = "/output"
	inputMountString = "src=" + miplMeshDirectory + ",target=" + dockerInputDirectory + ",type=bind"
	outputMountString = "src=" + outputDirectory + ",target=" + dockerOutputDirectory + ",type=bind"
	dockerInputPath = os.path.join(dockerInputDirectory, miplMeshFilename)
	dockerImage = "mipl-mesh-converter"

	subprocess.call(['docker', 'run', '-it', '--mount', inputMountString, '--mount', outputMountString, dockerImage, dockerInputPath, dockerOutputDirectory, rover])