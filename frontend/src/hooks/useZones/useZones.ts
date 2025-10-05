import { useUnit } from "effector-react"
import { stores, events } from "../../stores/zonesStore"

export const useZones = () => {
    const [availableZones, 
        restrictedZones,
        urbanZones,
        baseZones,
        sidewalks,
        allZones,
        hasBaseZone,
        setAvailableZones,
        setRestrictedZones,
        setUrbanZones,
        setBaseZones,
        setSidewalks
    ] = useUnit([
        stores.$availableZones, 
        stores.$restrictedZones, 
        stores.$urbanZones,
        stores.$baseZones,
        stores.$sidewalks, 
        stores.$allZones,
        stores.$hasBaseZone,
        events.setAvailableZones,
        events.setRestrictedZones,
        events.setUrbanZones,
        events.setBaseZones,
        events.setSidewalks
    ])

    return {
        availableZones,
        restrictedZones,
        urbanZones,
        baseZones,
        sidewalks,
        allZones,
        hasBaseZone,
        setAvailableZones,
        setRestrictedZones,
        setUrbanZones,
        setBaseZones,
        setSidewalks
    }
}