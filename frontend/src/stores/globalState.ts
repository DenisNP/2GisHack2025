import {stores as zonesStore, events as zonesEvents} from "./zonesStore"
import {stores as poiStores, events as poiEvents} from "../components/PoiManager/models"
import { combine, createEffect, createEvent, createStore, sample } from "effector"
import { convertToSnakeCase } from "../utils/convertToSnakeCase"
import { getAdjustedPoi } from "../utils/getAdjustedPoi"
import { ZoneData } from "../components/ZoneDrawer"
import { AddPoiEventData } from "../components/PoiManager/PoiManager.types"
import { RunSimulationRequest } from "../types/InternalApi"


type AllZones = {
    availabelZones: ZoneData[],
    restrictedZones: ZoneData[],
    urbanZones: ZoneData[],
    baseZones: ZoneData[],
}

type GlobalGeoStateProps = {
    zones: AllZones
    poi: AddPoiEventData[]
}

const SAVED_STATE_KEY = "saved_state";

const getJson = createEvent();
const saveCurrentState = createEvent();
const restoreState = createEvent();
const checkSavedState = createEvent(); // Новое событие для проверки

/**
 * Проверяет наличие и валидность сохраненного состояния в localStorage
 */
function validateSavedState(): boolean {
    try {
        const savedData = localStorage.getItem(SAVED_STATE_KEY);
        if (!savedData) {
            return false;
        }
        
        const state: GlobalGeoStateProps = JSON.parse(savedData);
        
        // Проверяем структуру объекта
        if (!state || typeof state !== 'object') {
            return false;
        }
        
        // Проверяем наличие обязательных полей
        if (!state.zones || !Array.isArray(state.poi)) {
            return false;
        }
        
        // Проверяем структуру zones
        const { zones } = state;
        if (!zones.availabelZones || !zones.restrictedZones || 
            !zones.urbanZones || !zones.baseZones) {
            return false;
        }
        
        return true;
    } catch (error) {
        console.error('Error validating saved state:', error);
        return false;
    }
}

/**
 * Store для отслеживания наличия валидного сохраненного состояния
 */
const $hasSavedState = createStore(validateSavedState());

/**
 * Effect для проверки сохраненного состояния
 */
const checkSavedStateFx = createEffect(() => {
    return validateSavedState();
});

// Обновляем store при изменении localStorage
$hasSavedState.on(checkSavedStateFx.doneData, (_, isValid) => isValid);


const getJsonFx = createEffect((state: RunSimulationRequest)=>{
    var adjustedPoi = getAdjustedPoi(state.poi, state.zones);
    console.log(convertToSnakeCase(JSON.stringify({...state, poi: adjustedPoi}, null, 2)));
})

const saveCurrentStateFx = createEffect((state: GlobalGeoStateProps)=>{
    try {
        localStorage.setItem(SAVED_STATE_KEY, JSON.stringify(state));
        console.log('state saved to localStorage:', state);
    } catch (error) {
        console.error('Failed to save state to localStorage:', error);
        throw error;
    }
})

const restoreStateFx = createEffect(() => {
    const savedData = localStorage.getItem(SAVED_STATE_KEY);
    if (savedData) {
        try {
            const state: GlobalGeoStateProps = JSON.parse(savedData);
            // Загружаем зоны из localStorage
            zonesEvents.setAvailableZones(state.zones.availabelZones || []);
            zonesEvents.setRestrictedZones(state.zones.restrictedZones || []);
            zonesEvents.setUrbanZones(state.zones.urbanZones || []);
            zonesEvents.setBaseZones(state.zones.baseZones || []);
            
            // Находим максимальный ID среди всех зон и устанавливаем следующий
            const allIds = [
                ...(state.zones.availabelZones || []).map(z => z.id),
                ...(state.zones.restrictedZones || []).map(z => z.id),
                ...(state.zones.urbanZones || []).map(z => z.id),
                ...(state.zones.baseZones || []).map(z => z.id)
            ];
            const maxId = allIds.length > 0 ? Math.max(...allIds) : 0;
            zonesEvents.setNewId(maxId + 1);

            state.poi.forEach(poi=>{
                poiEvents.addPoi(poi);
            });
            
        } catch (error) {
            console.error('Failed to parse saved state from localStorage:', error);
        }
    }
})

const $globalState = combine([zonesStore.$allZones, poiStores.$store], ([$allZones, poiStore]):RunSimulationRequest => ({
    zones: $allZones,
    poi: poiStore.poi
}))

sample({
    clock: getJson,
    source: $globalState,
    target: getJsonFx
})

sample({
    clock: saveCurrentState,
    source: { 
        availabelZones: zonesStore.$availableZones, 
        restrictedZones: zonesStore.$restrictedZones, 
        urbanZones: zonesStore.$urbanZones, 
        baseZones: zonesStore.$baseZones,
        poiStore: poiStores.$store
    },
    fn: ({availabelZones, restrictedZones, urbanZones, baseZones, poiStore}) : GlobalGeoStateProps => ({
        zones: {
            availabelZones,
            restrictedZones,
            urbanZones,
            baseZones
        },
        poi: poiStore.poi.map(p=>({geoPoint: p.geoPoint, type: p.type}))
    }),
    target: saveCurrentStateFx
})

sample({
    clock: restoreState,
    target: restoreStateFx
})

// Обновляем флаг после сохранения состояния
sample({
    clock: saveCurrentStateFx.done,
    target: checkSavedStateFx
})

// Обновляем флаг после восстановления состояния
sample({
    clock: restoreStateFx.done,
    target: checkSavedStateFx
})

// Проверяем состояние по запросу
sample({
    clock: checkSavedState,
    target: checkSavedStateFx
})

export const stores = {
    $globalState,
    $hasSavedState
}

export const events = {
    getJson,
    saveCurrentState,
    restoreState,
    checkSavedState
}