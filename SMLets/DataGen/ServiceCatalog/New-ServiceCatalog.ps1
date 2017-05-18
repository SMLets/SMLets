param ( $filename )
# filename is a csv file which represents the service catalog
./convert-so $filename
./convert-ro $filename
./link-roso
