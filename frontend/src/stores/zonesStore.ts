import { combine, createEvent, createStore, restore, sample } from "effector";
import { ZoneData } from "../components/ZoneDrawer";
import { Zone, ZoneType } from "../types/Zone";
import { MapStore, stores as mapStores } from "./mapStore";
import { projectPoint } from "../utils/pointsProjection";
import { GeoPoint } from "../types/GeoPoint";
import { events as poiEvents } from "../components/PoiManager/models";
import { events as mapEvents } from "./mapStore";

type AddZoneProps = {
    type: ZoneType,
    coords: GeoPoint[] 
}

const setAvailableZones = createEvent<ZoneData[]>()
const addAvailabelZone = createEvent<ZoneData>()
const setRestrictedZones = createEvent<ZoneData[]>()
const addRestrictedZone = createEvent<ZoneData>()
const setUrbanZones = createEvent<ZoneData[]>()
const addUrbanZone = createEvent<ZoneData>()
const setBaseZones = createEvent<ZoneData[]>()
const addBaseZone = createEvent<ZoneData>()
const incrementZoneId = createEvent<void>()
const setNewId = createEvent<number>()
const addZone = createEvent<AddZoneProps>()
const clearAllZones = createEvent()

const $availableZones = restore(setAvailableZones, []).on(addAvailabelZone, (state, zone) => [
    ...state,
    zone])
const $restrictedZones = restore(setRestrictedZones, []).on(addRestrictedZone, (state, zone) => [
    ...state,
    zone])
const $urbanZones = restore(setUrbanZones, []).on(addUrbanZone, (state, zone) => [
    ...state,
    zone])
const $baseZones = restore(setBaseZones, []).on(addBaseZone, (state, zone) => [
    ...state,
    zone])
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

const $allZones = combine([$baseZones, $availableZones, $restrictedZones, $urbanZones, mapStores.$mapStore], 
    ([baseZones, availableZones, restrictedZones, urbanZonde, mapStore]) => {
        if(!mapStore.map)
        {
            return [];
        }

    return [
        ...baseZones.map(x=>mapZoneData(x, ZoneType.None, mapStore)),
        ...availableZones.map(x=>mapZoneData(x, ZoneType.Available, mapStore)), 
        ...restrictedZones.map(x=>mapZoneData(x, ZoneType.Restricted, mapStore)),
        ...urbanZonde.map(x=>mapZoneData(x, ZoneType.Urban, mapStore)),
    ]
})

const $hasBaseZone = $baseZones.map(zones => zones.length > 0);

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
    clock: addZone,
    source: $newZoneId,
    filter: (_, data)=> data.type === ZoneType.None,
    fn: (newZoneId, {coords}): ZoneData =>({id: newZoneId, coords}),
    target: [addBaseZone, incrementZoneId]
})

sample({
    clock: setBaseZones,
    filter: (zones)=> zones.length === 0,
    target: clearAllZones
})

sample({
    clock: clearAllZones,
    fn: () => [],
    target: [setAvailableZones, setRestrictedZones, setUrbanZones, poiEvents.removeAllPoi],
})

sample({
    clock: setBaseZones,
    filter: (zones)=> zones.length === 0,
    fn: (zones)=> zones[0].coords[0],
    target: mapEvents.setOrigin
})

export const stores = {
    $availableZones,
    $restrictedZones,
    $urbanZones,
    $baseZones,
    $allZones,
    $newZoneId,
    $hasBaseZone,
}

export const events = {
    setAvailableZones,
    setRestrictedZones,
    setUrbanZones,
    setBaseZones,
    incrementZoneId,
    addZone,
    setNewId,
    clearAllZones
}