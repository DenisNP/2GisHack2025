import { createStore, createEvent } from 'effector';
import { Map as MapGl } from '@2gis/mapgl/types';
import { GeoPoint } from '../types/GeoPoint';

export type MapStore = { map?: MapGl, origin: GeoPoint };

const $mapStore = createStore<MapStore>({
    map: undefined,
    origin: { lng: 30.349213, lat: 59.937766 },
});

/*events*/
export const setMap = createEvent<MapGl>();
export const setOrigin = createEvent<GeoPoint>();

/*store*/
$mapStore.on(setMap, (state, map) => ({
    ...state,
    map
}));
$mapStore.on(setOrigin, (state, origin) => ({
    ...state,
    origin
}));

export const stores = { $mapStore };