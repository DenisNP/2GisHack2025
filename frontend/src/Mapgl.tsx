import { useEffect } from 'react';
import { load } from '@2gis/mapgl';
import { useMapglContext } from './MapglContext';
import {Map as MapGl} from '@2gis/mapgl/types';
// Ruler plugin removed — drawing is handled by PolygonDrawer
import { MapWrapper } from './MapWrapper';

export const MAP_CENTER = [30.349213, 59.937766];

export default function Mapgl() {
    const { setMapglContext } = useMapglContext();

    useEffect(() => {
        let map: MapGl | undefined = undefined;

        load().then((mapgl) => {
            map = new mapgl.Map('map-container', {
                center: MAP_CENTER,
                zoom: 18,
                key: process.env.REACT_APP_MAPGL_API_KEY || '',
            });

            setMapglContext({
                mapglInstance: map,
                mapgl,
            });
        });

        // Destroy the map, if Map component is going to be unmounted
        return () => {
            map && map.destroy();
            setMapglContext({ mapglInstance: undefined, mapgl: undefined });
        };
    }, [setMapglContext]);

    return (
        <>
            <MapWrapper />
        </>
    );
}
