#!/bin/bash

set -e

cat<<EOF

#######################################
#  BOOTSTRAP
#######################################

EOF

if ! dpkg -l | grep -q "sudo"; then
	apt-get install -yq sudo
fi

if ! dpkg -l | grep -q "net-tools"; then
	apt-get install -yq net-tools
fi

if ! dpkg -l | grep -q "dirmngr"; then
	apt-get install -yq dirmngr
fi

if ! dpkg -l | grep -q "debian-archive-keyring"; then
	apt-get install -yq debian-archive-keyring
fi

if ! dpkg -l | grep -q "debconf-utils"; then
	apt-get install -yq debconf-utils
fi
