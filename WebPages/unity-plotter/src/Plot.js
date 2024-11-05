import React from 'react';
import {
  ScatterChart,
  Scatter,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from 'recharts';

const colors = [
  '#8884d8',
  '#82ca9d',
  '#ffc658',
  '#d0ed57',
  '#a4de6c',
  '#8dd1e1',
  '#83a6ed',
  '#8e4585',
  '#ff6f61',
  '#6b5b95',
];

const Plot = ({ data, selectedVariables }) => {
  // Filter selected drone positions
  const dronePositions = selectedVariables.filter(
    (key) => key.includes('Drone') && 
            key.includes('position') 
  );

  // Group by drone
  const drones = {};
  dronePositions.forEach((key) => {
    const [droneKey] = key.split('_position');
    if (!drones[droneKey]) drones[droneKey] = {};
    drones[droneKey][key.split('_').pop()] = key;
  });

  return (
    <div>
      <h2>Drone Positions (X vs Z)</h2>
      <ScatterChart
        width={800}
        height={600}
        margin={{ top: 20, right: 20, bottom: 20, left: 20 }}
      >
        <CartesianGrid />
        <XAxis
          type="number"
          dataKey="x"
          name="X Position"
          label={{ value: 'X Position', position: 'insideBottomRight', offset: -10 }}
        />
        <YAxis
          type="number"
          dataKey="z"
          name="Z Position"
          label={{ value: 'Z Position', angle: -90, position: 'insideLeft' }}
        />
        <Tooltip cursor={{ strokeDasharray: '3 3' }} />
        <Legend />
        {Object.keys(drones).map((droneKey, index) => (
          <Scatter
            key={droneKey}
            name={droneKey}
            data={data.map((entry) => ({
              x: entry[`${droneKey}_position_x`],
              z: entry[`${droneKey}_position_z`],
            }))}
            fill={colors[index % colors.length]}
            line
          />
        ))}
      </ScatterChart>
    </div>
  );
};

export default Plot;
