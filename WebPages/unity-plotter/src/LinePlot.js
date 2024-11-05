import React from 'react';
import {
  LineChart,
  Line,
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

const LinePlot = ({ data, selectedVariables }) => {
  if (!data || data.length === 0 || selectedVariables.length === 0)
    return <p>No variables selected or data unavailable.</p>;

  // Prepare data with timestamps
  const plotData = data.map((entry) => {
    const newEntry = { timestamp: entry.timestamp };
    selectedVariables.forEach((variable) => {
      newEntry[variable] = entry[variable];
    });
    return newEntry;
  });

  return (
    <div>
      <h2>Selected Variables Over Time</h2>
      <LineChart
        width={800}
        height={400}
        data={plotData}
        margin={{ top: 20, right: 30, left: 20, bottom: 20 }}
      >
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="timestamp" tickFormatter={(tick) => tick.split(' ')[1]} />
        <YAxis />
        <Tooltip />
        <Legend />
        {selectedVariables.map((variable, index) => (
          <Line
            key={variable}
            type="monotone"
            dataKey={variable}
            stroke={colors[index % colors.length]}
            dot={false}
          />
        ))}
      </LineChart>
    </div>
  );
};

export default LinePlot;
