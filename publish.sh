#!/bin/bash

# Remove old unfolded-circle-oppo.tar.gz if it exists
rm -f ./unfolded-circle-oppo.tar.gz

# Remove old publish directory if it exists
rm -rf ./publish

# Clean build and publish directories using dotnet clean
echo "Clean"
dotnet clean -c Release -p:BuildForLinuxArm=true

# Run dotnet publish
echo "Publish"
dotnet publish ./src/UnfoldedCircle.OppoBluRay/UnfoldedCircle.OppoBluRay.csproj -c Release -p:BuildForLinuxArm=true -o ./publish

# Enter the publish directory
cd ./publish || exit

# Create a new directory called driver
mkdir -p driverdir

# Create bin, config, and data folders in the driver directory
mkdir -p ./driverdir/bin ./driverdir/config ./driverdir/data

# Copy driver.json to the root of the driver directory
cp ./driver.json ./driverdir/

# Copy icon to root of the driver directory
cp ../oppo.png ./driverdir/

# Copy appsettings*.json to the bin directory
cp ./appsettings*.json ./driverdir/bin/

# Copy driver (file) and *.pdb files from the publish directory to the bin directory in the driver directory
cp ./driver ./driverdir/bin/
cp ./*.pdb ./driverdir/bin/

# Package the driver directory into a tarball
cd ./driverdir || exit
tar -czvf ../../unfolded-circle-oppo.tar.gz ./*

# Remove the output directory
rm -rf ../../publish