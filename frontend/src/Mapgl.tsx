import { useEffect } from 'react';
import { load } from '@2gis/mapgl';
import { useMapglContext } from './MapglContext';
import { Map as MapGl } from '@2gis/mapgl/types';
// Ruler plugin removed — drawing is handled by PolygonDrawer
import { MapWrapper } from './MapWrapper';
import { useUnit } from 'effector-react';
import { events } from './stores/mapStore';

export const MAP_CENTER = [30.349213, 59.937766];

export default function Mapgl() {
    const { setMapglContext } = useMapglContext();
    const setMap = useUnit(events.setMap);

    useEffect(() => {
        let map: MapGl | undefined = undefined;

        load().then((mapgl) => {
            map = new mapgl.Map('map-container', {
                center: MAP_CENTER,
                zoom: 18,
                key: process.env.REACT_APP_MAPGL_API_KEY || '',
                style: process.env.REACT_APP_MAPGL_STYLE_ID,
            });

            setMapglContext({
                mapglInstance: map,
                mapgl,
            });
            setMap(map);
        });

        // Destroy the map, if Map component is going to be unmounted
        return () => {
            map && map.destroy();
            setMapglContext({ mapglInstance: undefined, mapgl: undefined });
            setMap(undefined);
        };
    }, [setMapglContext, setMap]);

    return (
        <>
            <MapWrapper />
        </>
    );
}
