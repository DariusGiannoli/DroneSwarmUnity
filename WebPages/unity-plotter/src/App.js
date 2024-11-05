import React, { useState, useEffect } from 'react';
import DroneMap from './DroneMap';
import VariableSelector from './VariableSelector';
import LinePlot from './LinePlot';

const App = () => {
  const [data, setData] = useState([]);
  const [variables, setVariables] = useState([]);
  const [selectedVariables, setSelectedVariables] = useState([]);
  const [mapStateVariables, setMapStateVariables] = useState([]);
  const [droneStaeVariables, setDroneStateVariables] = useState([]);

  useEffect(() => {
    // Fetch data from localhost:5000/data
    setInterval(() => {
    fetch('http://localhost:5000/data')
      .then((response) => response.json())
      .then((fetchedData) => {
        // Flatten the data
        const flatData = fetchedData.map((entry) => {
          const flattenedEntry = {};
          Object.keys(entry).forEach((key) => {
            if (Array.isArray(entry[key])) {
              const [x, y, z] = entry[key];
              flattenedEntry[`${key}_x`] = x;
              flattenedEntry[`${key}_y`] = y;
              flattenedEntry[`${key}_z`] = z;
            } else {
              flattenedEntry[key] = entry[key];
            }
          });
          return flattenedEntry;
        });

        setData(flatData);

        // Extract variables that are not related to drones
        const variableNames = Object.keys(flatData[0]).filter(
          (key) => !key.includes('Drone') && !key.includes('map_')  && key !== 'timestamp'
        );
        setVariables(variableNames);

        //extract only the map state
        const mapState = Object.keys(flatData[0]).filter(
          (key) => key.includes('map_')
        );
        setMapStateVariables(mapState);

        //extract only the drone state
        const droneState = Object.keys(flatData[0]).filter(
          (key) => key.includes('Drone')
        );
        setDroneStateVariables(droneState);

      })
      .catch((error) => console.error('Error fetching data:', error));
    }, 500);
  }, []);



  return (
    <div style={{ padding: '20px' }}>
      <h1>Dynamic Drone Data Plotter</h1>
      <DroneMap data={data} droneVariables={droneStaeVariables} mapVariables={mapStateVariables} />
      <VariableSelector
        variables={variables}
        selectedVariables={selectedVariables}
        setSelectedVariables={setSelectedVariables}
      />
      <LinePlot data={data} selectedVariables={selectedVariables} />
    </div>
  );
};

export default App;
