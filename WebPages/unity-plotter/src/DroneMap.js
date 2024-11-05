import React from 'react';
import {
  ScatterChart,
  Scatter,
  XAxis,
  YAxis,
  Tooltip,
  Customized,
} from 'recharts';

const DroneMap = ({ data, droneVariables, mapVariables }) => {
  if (!data || data.length === 0) return <p>Loading map...</p>;

  // Get the latest entry
  const latestEntry = data[data.length - 1];

  // Extract drone positions
  const drones = {}
  //foreach drone get Drone0_position_x, Drone0_position_z
  droneVariables.forEach((key) => {
    if(key.includes('position')){
      const droneName = key.split('_')[0];
      if (!drones[droneName]) drones[droneName] = {};
      const value = latestEntry[key];
      drones[droneName][key.split('_').pop()] = value;
    }
  });

  const dronesData = Object.keys(drones).map((droneKey) => ({
    name: droneKey,
    x: drones[droneKey].x,
    z: drones[droneKey].z
  }));

  


  

  // Extract map variables
  const mapCenterX = latestEntry['map_center_position_x'];
  const mapCenterZ = latestEntry['map_center_position_z'];
  const mapData = latestEntry['map_data'];
  const mapCellSize = latestEntry['map_cell_size'];

  if (
    mapCenterX === undefined ||
    mapCenterZ === undefined ||
    mapData === undefined ||
    mapCellSize === undefined
  ) {
    return <p>Map data is incomplete</p>;
  }

  const mapWidth = Math.sqrt(mapData.length);
  const mapHeight = Math.sqrt(mapData.length);

  // Prepare the cell data
  const cells = [];
  const cellSize = mapCellSize;
  const halfWidth = (mapWidth * cellSize) / 2;
  const halfHeight = (mapHeight * cellSize) / 2;
  for (let i = 0; i < mapHeight; i++) {
    for (let j = 0; j < mapWidth; j++) {
      const x = mapCenterX - halfWidth + j * cellSize;
      const z = mapCenterZ - halfHeight + i * cellSize;
      const value = mapData[i * mapWidth + j];
      cells.push({ x, z, value });
    }
  }

  // Determine the bounds of the map for scaling
  const xMin = mapCenterX - halfWidth;
  const xMax = mapCenterX + halfWidth;
  const zMin = mapCenterZ - halfHeight;
  const zMax = mapCenterZ + halfHeight;

  console.log(drones);


  return (
    <div>
      <h2>Drone Map</h2>
      <ScatterChart
        width={800}
        height={600}
        margin={{ top: 20, right: 20, bottom: 20, left: 20 }}
      >
        <XAxis
          type="number"
          dataKey="x"
          domain={[xMin, xMax]}
          label={{ value: 'X Position', position: 'insideBottomRight', offset: -10 }}
          tick={false}
        />
        <YAxis
          type="number"
          dataKey="z"
          domain={[zMin, zMax]}
          label={{ value: 'Z Position', angle: -90, position: 'insideLeft' }}
          tick={false}
        />
        <Tooltip cursor={{ strokeDasharray: '3 3' }} />
        {/* Render the grid cells */}
        <Customized component={(props) => <GridCells {...props} cells={cells} cellSize={cellSize} />} />
        {/* Render the drone positions */}
        <Scatter
          name="Drones"
          data={dronesData}
          fill="#ff0000"
          shape="circle"
          legendType="circle"
        />
      </ScatterChart>
    </div>
  );
};

const GridCells = (props) => {
  const { cells, cellSize, xAxisMap, yAxisMap } = props;

  const xAxis = xAxisMap[0]; // Assuming default xAxis
  const yAxis = yAxisMap[0]; // Assuming default yAxis

  const xScale = xAxis.scale;
  const yScale = yAxis.scale;

  return (
    <g>
      {cells.map((cell, index) => {
        const { x, z, value } = cell;
        const fill = value == 1 ? '#000000' : '#FFFFFF';

        // Map data coordinates to pixel positions
        const xPos = xScale(x);
        const yPos = yScale(z + cellSize);
        const width = xScale(x + cellSize) - xScale(x);
        const height = yScale(z) - yScale(z + cellSize);

        return (
          <rect
            key={`cell-${index}`}
            x={xPos}
            y={yPos}
            width={width}
            height={height}
            fill={fill}
            stroke="#ccc"
            strokeWidth={0.5}
          />
        );
      })}
    </g>
  );
};

export default DroneMap;
