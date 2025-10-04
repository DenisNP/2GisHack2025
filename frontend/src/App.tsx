import './App.css';
import React, { useState } from 'react';
import Mapgl from './Mapgl';
import { MapglContextProvider } from './MapglContext';
import ButtonResetMapCenter from './ButtonResetMapCenter';
import {PolygonDrawer, PolygonData } from './components/PolygonDrawer';
import { UrbanDrawer, SidewalkData } from './components/UrbanDrawer';

function App() {
    // controlled polygon lists for two types
    const [polygonsA, setPolygonsA] = useState<PolygonData[]>([]);
    const [polygonsB, setPolygonsB] = useState<PolygonData[]>([]);
    
    // controlled sidewalks list
    const [sidewalks, setSidewalks] = useState<SidewalkData[]>([]);

    return (
        <MapglContextProvider>
            <div>
                <div className='App-buttons'>
                    <div className='App-button-item'>
                        <ButtonResetMapCenter />
                    </div>
                </div>

                <div className='App-map-container'>
                    <Mapgl />
                    {/* Two controlled drawers with different colors/labels */}
                    <PolygonDrawer color='#cc3b3b' label='Тип A' polygons={polygonsA} onPolygonsChanged={setPolygonsA} position={{ left: 12, top: 12 }} />
                    <PolygonDrawer color='#00b450' label='Тип B' polygons={polygonsB} onPolygonsChanged={setPolygonsB} position={{ left: 260, top: 12 }} />
                    {/* Urban drawer for sidewalks */}
                    <UrbanDrawer width={2} color='#FFD700' label='Тротуары' sidewalks={sidewalks} onSidewalksChanged={setSidewalks} position={{ left: 12, top: 350 }} />
                </div>
            </div>
        </MapglContextProvider>
    );
}

export default App;
