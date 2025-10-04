import { combine, createEffect, createEvent, createStore, restore, sample } from "effector";
import { ZoneData } from "../components/ZoneDrawer";
import { SidewalkData } from "../components/UrbanDrawer";
import { Zone, ZoneType } from "../types/Zone";
import { MapStore, stores as mapStores } from "./mapStore";
import { projectPoint } from "../utils/pointsProjection";
import { GeoPoint } from "../types/GeoPoint";

type AddZoneProps = {
    type: ZoneType,
    coords: GeoPoint[] 
}

type AllZones = {
    availabelZones: ZoneData[],
    restrictedZones: ZoneData[],
    urbanZones: ZoneData[],
}

const SAVED_ZONES_KEY = "saved_zones";

const setAvailableZones = createEvent<ZoneData[]>()
const addAvailabelZone = createEvent<ZoneData>()
const setRestrictedZones = createEvent<ZoneData[]>()
const addRestrictedZone = createEvent<ZoneData>()
const setUrbanZones = createEvent<ZoneData[]>()
const addUrbanZone = createEvent<ZoneData>()
const setSidewalks = createEvent<SidewalkData[]>()
const incrementZoneId = createEvent<void>()
const setNewId = createEvent<number>()
const addZone = createEvent<AddZoneProps>()
const saveZones = createEvent()
const restoreZones = createEvent()

const saveZonesFx = createEffect((allZones: AllZones) => {
    try {
        localStorage.setItem(SAVED_ZONES_KEY, JSON.stringify(allZones));
        console.log('Zones saved to localStorage:', allZones);
    } catch (error) {
        console.error('Failed to save zones to localStorage:', error);
        throw error;
    }
})

const restoreZonesFx = createEffect(() => {
    try {
        const savedData = localStorage.getItem(SAVED_ZONES_KEY);
        
        if (savedData) {
            const zones: AllZones = JSON.parse(savedData);
            console.log('Zones loaded from localStorage:', zones);
            
            // Загружаем зоны из localStorage
            setAvailableZones(zones.availabelZones || []);
            setRestrictedZones(zones.restrictedZones || []);
            setUrbanZones(zones.urbanZones || []);
            
            // Находим максимальный ID среди всех зон и устанавливаем следующий
            const allIds = [
                ...(zones.availabelZones || []).map(z => z.id),
                ...(zones.restrictedZones || []).map(z => z.id),
                ...(zones.urbanZones || []).map(z => z.id)
            ];
            const maxId = allIds.length > 0 ? Math.max(...allIds) : 0;
            setNewId(maxId + 1);
            
            console.log('Zones restored successfully');
        } else {
            console.log('No saved zones found in localStorage');
        }
    } catch (error) {
        console.error('Failed to restore zones from localStorage:', error);
        throw error;
    }
})

const $availableZones = restore(setAvailableZones, []).on(addAvailabelZone, (state, zone) => [
    ...state,
    zone])
const $restrictedZones = restore(setRestrictedZones, []).on(addRestrictedZone, (state, zone) => [
    ...state,
    zone])
const $urbanZones = restore(setUrbanZones, []).on(addUrbanZone, (state, zone) => [
    ...state,
    zone])
const $sidewalks = restore(setSidewalks, [])
const $newZoneId = createStore(1)
.on(incrementZoneId, (state) => state + 1)
.on(setNewId, (_, newId)=>newId);

const mapZoneData = (zoneData: ZoneData, zoneType: ZoneType, mapStore: MapStore): Zone => {
    if(!mapStore.map)
    {
        return {
            id: -1,
            type: ZoneType.Restricted,
            region: []
        }
    }
    return {
        id: zoneData.id,
        type: zoneType,
        region: zoneData.coords.map(geoPoint => projectPoint(geoPoint, mapStore.origin, mapStore.map!)),
    }
}

const $allZones = combine([$availableZones, $restrictedZones, $urbanZones, $sidewalks, mapStores.$mapStore], 
    ([availableZones, restrictedZones, urbanZonde, sidewalks, mapStore]) => {
        if(!mapStore.map)
        {
            return [];
        }

    return [
        ...availableZones.map(x=>mapZoneData(x, ZoneType.Available, mapStore)), 
        ...restrictedZones.map(x=>mapZoneData(x, ZoneType.Restricted, mapStore)),
        ...urbanZonde.map(x=>mapZoneData(x, ZoneType.Urban, mapStore)),
        ...sidewalks.map(x=>mapZoneData({id: x.id, coords: x.polygon}, ZoneType.Urban, mapStore))
    ]
})

sample({
    clock: addZone,
    source: $newZoneId,
    filter: (_, data)=> data.type === ZoneType.Available,
    fn: (newZoneId, {coords}): ZoneData =>({id: newZoneId, coords}),
    target: [addAvailabelZone, incrementZoneId]
})

sample({
    clock: addZone,
    source: $newZoneId,
    filter: (_, data)=> data.type === ZoneType.Restricted,
    fn: (newZoneId, {coords}): ZoneData =>({id: newZoneId, coords}),
    target: [addRestrictedZone, incrementZoneId]
})

sample({
    clock: addZone,
    source: $newZoneId,
    filter: (_, data)=> data.type === ZoneType.Urban,
    fn: (newZoneId, {coords}): ZoneData =>({id: newZoneId, coords}),
    target: [addUrbanZone, incrementZoneId]
})

sample({
    clock: saveZones,
    source: {availabelZones: $availableZones, restrictedZones: $restrictedZones, urbanZones: $urbanZones},
    target: saveZonesFx
})

sample({
    clock: restoreZones,
    target: restoreZonesFx
})

export const stores = {
    $availableZones,
    $restrictedZones,
    $urbanZones,
    $sidewalks,
    $allZones,
    $newZoneId
}

export const events = {
    setAvailableZones,
    setRestrictedZones,
    setUrbanZones,
    setSidewalks,
    incrementZoneId,
    addZone,
    saveZones,
    restoreZones
}