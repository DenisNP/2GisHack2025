import {stores as zonesStore, events as zonesEvents} from "./zonesStore"
import {stores as poiStores, events as poiEvents} from "../components/PoiManager/models"
import { combine, createEffect, createEvent, sample } from "effector"
import { Zone } from "../types/Zone"
import { Poi } from "../types/Poi"
import { convertToSnakeCase } from "../utils/convertToSnakeCase"
import { getAdjustedPoi } from "../utils/getAdjustedPoi"
import { ZoneData } from "../components/ZoneDrawer"
import { AddPoiEventData } from "../components/PoiManager/PoiManager.types"

type GlobalStateProps = {
    zones: Zone[],
    poi: Poi[]
}

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


const getJsonFx = createEffect((state: GlobalStateProps)=>{
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

const $globalState = combine([zonesStore.$allZones, poiStores.$store], ([$allZones, poiStore]):GlobalStateProps => ({
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

export const stores = {
    $globalState
}

export const events = {
    getJson,
    saveCurrentState,
    restoreState
}