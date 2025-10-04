import { createEffect, createEvent, createStore, sample } from "effector";
import {
    AddPoiEventData,
    EffectProps,
    MovePoiEventData,
    PoiManagerStore,
} from '../PoiManager.types';
import { Poi } from '../../../types/Poi';
import { projectPoint } from '../../../utils/pointsProjection';
import { EMPTY_POI, weightByType } from '../PoiManager.constants';
import { stores as mapStores } from '../../../stores/mapStore';

/*events*/
const addPoi = createEvent<AddPoiEventData>();
const removePoiById = createEvent<number>();
const removeAllPoi = createEvent<void>();
const movePoi = createEvent<MovePoiEventData>();

/*effects*/
const addPoiFx = createEffect(({data, store, mapStore}: EffectProps<AddPoiEventData, PoiManagerStore>): Poi => {
    if (mapStore.map === undefined) {
        return EMPTY_POI;
    }
    const geomPoint = projectPoint(data.geoPoint, mapStore.origin, mapStore.map);
    return {
        id: findNewId(store.poi),
        weight: weightByType(data.type),
        point: geomPoint,
        geoPoint: data.geoPoint,
        type: data.type,
    };
});

const findNewId = (pois: Poi[]): number => {
    const allIds = new Set<number>(pois.map((p) => p.id));
    let newId = 1;
    while (newId < Number.MAX_SAFE_INTEGER) {
        if (allIds.has(newId)) {
            newId++;
        } else {
            return newId;
        }
    }
    return -1;
};

/*stores*/
const $store = createStore<PoiManagerStore>({
    poi: []
});
$store.on(addPoiFx.doneData, (state, poi) => ({...state, poi: [...state.poi, poi]}));
$store.on(removePoiById, (state, poiId) => {
   const newPoi = state.poi.filter((p) => p.id !== poiId);
   return { poi: newPoi };
});
$store.on(removeAllPoi, () => ({ poi: [] }));

/*bindings*/
sample({
    clock: addPoi,
    target: addPoiFx,
    source: { store: $store, mapStore: mapStores.$mapStore },
    fn: ({ store, mapStore }, data): EffectProps<AddPoiEventData, PoiManagerStore> => ({ data, store, mapStore })
});

sample({
    clock: movePoi,
    source: { store: $store, mapStore: mapStores.$mapStore },
    fn: ({ store, mapStore }, data): PoiManagerStore => {
        const oldPoi = store.poi.find((p) => p.id === data.id);
        if (oldPoi !== undefined && mapStore.map !== undefined) {
            const newPois = store.poi.filter((p) => p.id !== data.id);

            const geomPoint = projectPoint(data.geoPoint, mapStore.origin, mapStore.map);
            const newPoi = {
                id: oldPoi.id,
                weight: oldPoi.weight,
                geoPoint: data.geoPoint,
                point: geomPoint,
                type: oldPoi.type,
            };

            return { poi: [...newPois, newPoi] };
        }

        return store;
    },
    target: $store,
});

/*exports*/
export const events = {
    addPoi,
    removePoiById,
    removeAllPoi,
    movePoi,
};

export const stores = {
    $store,
};