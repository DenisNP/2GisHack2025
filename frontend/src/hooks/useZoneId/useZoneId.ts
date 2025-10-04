import { useUnit } from "effector-react"
import { useRef, useEffect } from "react"
import { stores, events } from "../../stores/zonesStore"

export const useZoneId = () => {

    const [newZoneId, incrementZoneId] = useUnit([stores.$newZoneId, events.incrementZoneId])
    const newZoneIdRef = useRef(newZoneId);
    useEffect(() => {
        newZoneIdRef.current = newZoneId;
    }, [newZoneId]);

    const getNewZoneId = () => {
        var zoneId = newZoneIdRef.current;
        incrementZoneId();
        return zoneId;
    }

    return getNewZoneId
}