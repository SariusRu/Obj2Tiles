# Documentation
## Workflow
### Load File
- In a first step, the file is loaded. This takes place in the CsvProcessor Class
- The file is loaded and the first line (headers) is analyzed to find the columns for "type", "_x". "_Y" and "_z" as well as "grid_id"
- After that, all lines are parsed and stored in a CsvInformationHolder

### Analyze Grid
The grids (obtained using the grid_id) are analyzed.
For each calculated GridLevel, an item is added to the LodLevel Dictionary

### Populate LOD 0
We add a new GridField for every LOD 0 Tile. We store a subset of a CsvInformationHolder in that file.

### Load Models
We load and tile the 3d models required for this model

### Tile Level0Tiles
As the Level0 tiles are different to all other tile LODs, we need a seperate function to do this Level compared to other LOD-Levels.

For each GridField (= LOD0-Tile), we do the following:
  1. Load the tiled 3D Object and load all relevant information
  2. Check if the retrieved item is indeed a GridField
  3. Store the path to the tileset to be created in the GridField
  4. Create the directory
  5. Call the Tiler for LOD0
  6. We calculate the baseError as the diagonal of all elements in the GridField. If the BaseErrror of a tiled object is bigger than what was calculated, the bigger value is used.
  7. Calculate the Bounding Volume as the Minimum/Maximum of the gridfield +/- the offset of the biggest bounding volume of the tiled 3DObject
  8. For LOD0: The Root Tile has no Transformation, only the objects within the tile have a transformation
  9. We check the tileset: We remove all empty lists and set them to null
  10. We write the tile and check the written tile using the 3d-tiles-validator


