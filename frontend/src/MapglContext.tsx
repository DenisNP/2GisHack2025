import { createContext, useContext, useState, ReactNode, Dispatch, SetStateAction } from 'react';
import {Map as MapGl} from '@2gis/mapgl/types';

const MapglContext = createContext<{
    mapgl?: typeof mapgl;
    mapglInstance?: MapGl;
    setMapglContext: Dispatch<SetStateAction<MapContextState>>;
}>({
    mapgl: undefined,
    mapglInstance: undefined,
    setMapglContext: () => {},
});

interface MapContextState {
    mapglInstance?: MapGl;
    mapgl?: typeof mapgl;
}

export function useMapglContext() {
    return useContext(MapglContext);
}

export function MapglContextProvider({ children }: { children: ReactNode }) {
    const [{ mapglInstance, mapgl }, setMapglContext] = useState<MapContextState>({
        mapglInstance: undefined,
        mapgl: undefined,
    });
    return (
        <MapglContext.Provider value={{ mapgl, mapglInstance, setMapglContext }}>
            {children}
        </MapglContext.Provider>
    );
}
