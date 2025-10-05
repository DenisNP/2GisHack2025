import { createEffect, createEvent, createStore, sample } from "effector";
import {
    AddPoiEventData,
    EffectProps,
    MovePoiEventData,
    PoiManagerStore,
} from '../PoiManager.types';
import { Poi } from '../../../types/Poi';
import { projectPoint } from '../../../utils/pointsProjection';
import { DIST_TOLERANCE, EMPTY_POI, weightByType } from '../PoiManager.constants';
import { stores as mapStores } from '../../../stores/mapStore';
import { distance } from '../../../utils/getDistance';

/*events*/
const addPoi = createEvent<AddPoiEventData>();
const removePoiById = createEvent<number>();
const removeAllPoi = createEvent<void>();
const movePoi = createEvent<MovePoiEventData>();

/*effects*/
const addPoiFx = createEffect(({data, store, mapStore}: EffectProps<AddPoiEventData, PoiManagerStore>): Poi | null => {
    if (mapStore.map === undefined) {
        return EMPTY_POI;
    }
    const geomPoint = projectPoint(data.geoPoint, mapStore.origin, mapStore.map);

    // Перебираем существующие точки и ищем, не добавлена ли уже рядом
    const nearest = store.poi.find((p) => distance(p.point, geomPoint) <= DIST_TOLERANCE);

    // Если ближайшей нет, просто добавляем новую
    if (!nearest) {
        return {
            id: findNewId(store.poi),
            weight: weightByType(data.type),
            point: geomPoint,
            geoPoint: data.geoPoint,
            type: data.type,
        };
    // Иначе нужно оставить с более приоритетным типом
    } else {
        const nearestWeight = weightByType(nearest.type);
        const newWeight = weightByType(data.type);

        if (nearestWeight >= newWeight) {
            return null; // не добавляем новую точку
        } else {
            // вернём старую с новым типом
            return {
                id: nearest.id,
                weight: newWeight,
                point: nearest.point,
                geoPoint: nearest.geoPoint,
                type: nearest.type,
            }
        }
    }
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
$store.on(addPoiFx.doneData, (state, poi) => {
  if (poi === null) {
      return state;
  } else {
      const excluded = state.poi.filter((p) => p.id !== poi.id);
      return {...state, poi: [...excluded, poi]};
  }
});
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