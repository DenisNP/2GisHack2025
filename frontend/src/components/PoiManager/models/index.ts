import { createEffect, createEvent, createStore, sample } from "effector";
import { AddPoiData, EffectProps, PoiManagerStore } from '../PoiManager.types';
import { Poi } from '../../../types/Poi';
import { projectPoint } from '../../../utils/pointsProjection';
import { EMPTY_POI } from '../PoiManager.constants';
import { stores as mapStores } from '../../../stores/mapStore';

/*events*/
const addPoi = createEvent<AddPoiData>();

/*effects*/
const addPoiFx = createEffect(({data, store, mapStore}: EffectProps<AddPoiData, PoiManagerStore>): Poi => {
    if (mapStore.map === undefined) {
        return EMPTY_POI;
    }
    const geomPoint = projectPoint(data.geoPoint, mapStore.origin, mapStore.map);
    return { id: store.poi.length + 1, weight: data.weight, point: geomPoint };
});

/*stores*/
const $store = createStore<PoiManagerStore>({
    poi: []
});
$store.on(addPoiFx.doneData, (state, poi) => ({...state, poi: [...state.poi, poi]}));

/*bindings*/
sample({
    clock: addPoi,
    target: addPoiFx,
    source: { store: $store, mapStore: mapStores.$mapStore },
    fn: ({ store, mapStore }, data): EffectProps<AddPoiData, PoiManagerStore> => ({ data, store, mapStore })
});