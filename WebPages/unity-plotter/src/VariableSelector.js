import React from 'react';

const VariableSelector = ({ variables, selectedVariables, setSelectedVariables }) => {
  const handleChange = (e) => {
    const { value, checked } = e.target;
    if (checked) {
      setSelectedVariables([...selectedVariables, value]);
    } else {
      setSelectedVariables(selectedVariables.filter((item) => item !== value));
    }
  };

  return (
    <div>
      <h2>Select Variables to Plot Over Time:</h2>
      <div style={{ display: 'flex', flexWrap: 'wrap' }}>
        {variables.map((variable) => (
          <div key={variable} style={{ marginRight: '15px' }}>
            <input
              type="checkbox"
              value={variable}
              checked={selectedVariables.includes(variable)}
              onChange={handleChange}
            />
            <label>{variable}</label>
          </div>
        ))}
      </div>
    </div>
  );
};

export default VariableSelector;
