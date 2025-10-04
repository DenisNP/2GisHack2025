import React, { useState, useEffect, useRef } from 'react';
import './GraphVisualization.css';
import {antAlgorithmApi, AntAlgorithmClient, AntAlgorithmUtils} from './antAlgorithmApi';

async function getGraphData(edges) {
    try {
        // Prepare POIs for ant algorithm
        //const pois = nodes.map(it => AntAlgorithmUtils.createPoi(it.id, AntAlgorithmUtils.createPoint(it.point.x, it.point.y), it.weight));

        // Get ant algorithm results
        const results = await antAlgorithmApi.getAntAlgoInfo(edges);
        console.log('Ant algorithm results:', results);
        return results;        
    } catch (error) {
        if (error.name === 'ApiError') {
            console.error(`API Error (${error.status}):`, error.response);
        } else {
            console.error('Error:', error.message);
        }
    }
}

function getMidpoint(first, second) {
    return (first + second) / 2;
}

function getLenght(x1, x2, y1, y2) {
    const deltaX = x2 - x1;
    const deltaY = y2 - y1;
    return Math.sqrt(deltaX * deltaX + deltaY * deltaY);
}

const GraphVisualization = () => {
  const [graphData, setGraphData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const svgRef = useRef(null);
  const [selectedNode, setSelectedNode] = useState(null);

  useEffect(() => {
    loadGraphData();
  }, []);

    // let nodes = [
    //     { id: 1, weight: 0, point: { x: 100, y: 100 } },
    //     { id: 2, weight: 0, point: { x: 300, y: 150 } },
    //     { id: 3, weight: 0, point: { x: 200, y: 300 } },
    //     { id: 4, weight: 0, point: { x: 400, y: 250 } },
    //     { id: 5, weight: 4, point: { x: 500, y: 100 } },
    //     { id: 6, weight: 4, point: { x: 500, y: 300 } },
    //     { id: 7, weight: 0, point: { x: 100, y: 200 } },
    //     { id: 8, weight: 0, point: { x: 360, y: 200 } },
    //     { id: 9, weight: 9, point: { x: 321, y: 456 } },
    //     { id: 10, weight: 0, point: { x: 789, y: 654 } },
    //     { id: 11, weight: 9, point: { x: 951, y: 159 } },
    //     { id: 12, weight: 0, point: { x: 753, y: 357 } },
    // ];


    let nodes = [];
    
    for(let i = 0; i < 100; i++) {
        nodes.push({ id: i, weight: Math.random(), point: { x: Math.floor((Math.random() * 1500)), y: Math.floor((Math.random() * 800)) } });
    }
    
    let edges = [];
    
    for (let i = 0; i < nodes.length - 1; i++) {
        const node = nodes[i];
        if (node.id % 10 === 1) {
            edges.push( {from: node, to: nodes[i+1]} )
        }
    }

  const loadGraphData = async () => {
    try {
      setLoading(true);
      const data = await getGraphData(edges);
      setGraphData(data);
    } catch (err) {
      setError('Ошибка при загрузке данных графа');
      console.error('Error loading graph data:', err);
    } finally {
      setLoading(false);
    }
  };

  // Функция для расчета толщины линии на основе веса
  const calculateStrokeWidth = (weight) => {
    const minWeight = 0;
    const maxWeight = Math.max(...graphData.map(edge => edge.weight));
    const minWidth = 1;
    const maxWidth = 8;

    return minWidth + ((weight - minWeight) / (maxWeight - minWeight)) * (maxWidth - minWidth);
  };

  // Функция для расчета цвета линии на основе веса
  const calculateStrokeColor = (weight) => {
    const maxWeight = Math.max(...graphData.map(edge => edge.weight));
    const intensity = Math.floor((weight / maxWeight) * 255);
    return `rgb(${intensity}, 100, 100)`;
  };

  if (loading) {
    return (
        <div className="graph-container">
          <div className="loading">Загрузка графа...</div>
        </div>
    );
  }

  if (error) {
    return (
        <div className="graph-container">
          <div className="error">{error}</div>
          <button onClick={loadGraphData} className="retry-button">
            Попробовать снова
          </button>
        </div>
    );
  }

  if (!graphData) {
    return (
        <div className="graph-container">
          <div className="error">Данные графа не загружены</div>
        </div>
    );
  }

  return (
      <div className="graph-app">
        <div className="graph-header">
          <h1>Визуализатор графов</h1>
          <button onClick={loadGraphData} className="refresh-button">
            Обновить граф
          </button>
        </div>

        <div className="graph-info">
          {selectedNode && (
              <div className="node-info">
                Выбрана вершина: <strong>{selectedNode.id}</strong> (ID: {selectedNode.id})
              </div>
          )}
        </div>

        <div className="graph-container">
          <svg
              ref={svgRef}
              width="1900"
              height="800"
              className="graph-svg"
          >
            {/* Рисуем рёбра */}
            {graphData.map((edge, index) => {
                    
              const sourceNode = nodes.find(node => node.id === edge.from.id);
              const targetNode = nodes.find(node => node.id === edge.to.id);

              if (!sourceNode || !targetNode) return null;

              const strokeWidth = calculateStrokeWidth(edge.weight);
              const strokeColor = calculateStrokeColor(edge.weight);
              
              const lineLenght = getLenght(sourceNode.point.x, targetNode.point.x, sourceNode.point.y, targetNode.point.y);

              return (
                  <g key={edge.id}>
                  <line
                      key={`edge-${index}`}
                      x1={sourceNode.point.x}
                      y1={sourceNode.point.y}
                      x2={targetNode.point.x}
                      y2={targetNode.point.y}
                      stroke={strokeColor}
                      strokeWidth={strokeWidth}
                      className="graph-edge"
                  />
                  <text
                      x={getMidpoint(sourceNode.point.x, targetNode.point.x)}
                      y={getMidpoint(sourceNode.point.y, targetNode.point.y)}
                      textAnchor="middle"
                      dy=".3em"
                      fill="black"
                      fontSize="12"
                      fontWeight="bold"
                      pointerEvents="none"
                  >
                      {edge.weight.toFixed(2) + ", " + lineLenght.toFixed(2)}
                  </text>
                  </g>
              );
            })}

            {/* Рисуем вершины */}
            {nodes.map((node) => (
                <g key={node.id}>
                  <circle
                      cx={node.point.x}
                      cy={node.point.y}
                      r="20"
                      fill={selectedNode?.id === node.id ? "#4CAF50" : "#2196F3"}
                      stroke="#1976D2"
                      strokeWidth="2"
                      className="graph-node"
                      onClick={() => setSelectedNode(node)}
                      style={{ cursor: 'pointer' }}
                  />
                  <text
                      x={node.point.x}
                      y={node.point.y}
                      textAnchor="middle"
                      dy=".3em"
                      fill="white"
                      fontSize="12"
                      fontWeight="bold"
                      pointerEvents="none"
                  >
                    {node.id}
                  </text>
                </g>
            ))}
          </svg>
        </div>

        <div className="legend">
          <h3>Легенда:</h3>
          <div className="legend-items">
            <div className="legend-item">
              <div className="legend-color node-color"></div>
              <span>Вершина графа</span>
            </div>
            <div className="legend-item">
              <div className="legend-color edge-thin"></div>
              <span>Лёгкое ребро (малый вес)</span>
            </div>
            <div className="legend-item">
              <div className="legend-color edge-thick"></div>
              <span>Тяжёлое ребро (большой вес)</span>
            </div>
          </div>
        </div>
      </div>
  );
};

export default GraphVisualization;