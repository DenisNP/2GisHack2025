import { useUnit } from "effector-react"
import { stores, events } from "../../stores/zonesStore"

export const useZones = () => {
    const [availableZones, 
        restrictedZones,
        urbanZones,
        sidewalks,
        allZones,
        setAvailableZones,
        setRestrictedZones,
        setUrbanZones,
        setSidewalks
    ] = useUnit([
        stores.$availableZones, 
        stores.$restrictedZones, 
        stores.$urbanZones,
        stores.$sidewalks, 
        stores.$allZones,
        events.setAvailableZones,
        events.setRestrictedZones,
        events.setUrbanZones,
        events.setSidewalks
    ])

    return {
        availableZones,
        restrictedZones,
        urbanZones,
        sidewalks,
        allZones,
        setAvailableZones,
        setRestrictedZones,
        setUrbanZones,
        setSidewalks
    }
}