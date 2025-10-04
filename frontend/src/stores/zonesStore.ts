import { combine, createEvent, createStore, restore } from "effector";
import { ZoneData } from "../components/ZoneDrawer";
import { SidewalkData } from "../components/UrbanDrawer";
import { Zone, ZoneType } from "../types/Zone";
import { MapStore, stores as mapStores } from "./mapStore";
import { projectPoint } from "../utils/pointsProjection";

const setAvailableZones = createEvent<ZoneData[]>()
const setRestrictedZones = createEvent<ZoneData[]>()
const setUrbanZones = createEvent<ZoneData[]>()
const setSidewalks = createEvent<SidewalkData[]>()
const incrementZoneId = createEvent<void>()

const $availableZones = restore(setAvailableZones, []) 
const $restrictedZones = restore(setRestrictedZones, [])
const $urbanZones = restore(setUrbanZones, [])
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
}