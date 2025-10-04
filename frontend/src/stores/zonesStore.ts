import { combine, createEvent, createStore, restore, sample } from "effector";
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

const setAvailableZones = createEvent<ZoneData[]>()
const addAvailabelZone = createEvent<ZoneData>()
const setRestrictedZones = createEvent<ZoneData[]>()
const addRestrictedZone = createEvent<ZoneData>()
const setUrbanZones = createEvent<ZoneData[]>()
const addUrbanZone = createEvent<ZoneData>()
const setSidewalks = createEvent<SidewalkData[]>()
const incrementZoneId = createEvent<void>()
const addZone = createEvent<AddZoneProps>()

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
const $newZoneId = createStore(1).on(incrementZoneId, (state) => state + 1);

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
    filter: (_, data)=> data.type !== ZoneType.Available,
    fn: (newZoneId, {coords}): ZoneData =>({id: newZoneId, coords}),
    target: [addAvailabelZone, incrementZoneId]
})

sample({
    clock: addZone,
    source: $newZoneId,
    filter: (_, data)=> data.type !== ZoneType.Restricted,
    fn: (newZoneId, {coords}): ZoneData =>({id: newZoneId, coords}),
    target: [addRestrictedZone, incrementZoneId]
})

sample({
    clock: addZone,
    source: $newZoneId,
    filter: (_, data)=> data.type !== ZoneType.Urban,
    fn: (newZoneId, {coords}): ZoneData =>({id: newZoneId, coords}),
    target: [addUrbanZone, incrementZoneId]
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
    addZone
}