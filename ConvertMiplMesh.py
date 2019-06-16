#!/usr/bin/python

import os
import sys
import argparse
import subprocess


def parseArgv():
	parser = argparse.ArgumentParser(description="Convert a MIPL .pfb or .iv mesh for MER or MSL to .obj format.  ", 
		epilog="E.g., ConvertMiplMesh --develop --updatetestserver --staging --merb --windows --overwrite-patcher " + \
		"-c -v auto -p previous current -o --run-on-osx-only -l 100")

	parser.add_argument("-i", "--mipl-mesh-path", metavar=('MIPL_MESH_PATH'), 
		help="The path of the mipl mesh to convert.")

	parser.add_argument("-d", "--output-directory", metavar=('OUTPUT_DIR'), 
		help="The directory to write the obj file to.")

	rovergroup = parser.add_mutually_exclusive_group(required=True)
	rovergroup.add_argument("--merb", action="store_true")
	rovergroup.add_argument("--msl", action="store_true")

	args = parser.parse_args()

	return args



def main():
		#parse out dirs, check exist, make docker command with mounts. make a command to run on the docker container

		
