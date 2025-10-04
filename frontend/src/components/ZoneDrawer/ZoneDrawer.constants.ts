import { ZoneType } from "../../types/Zone";

export const ZONE_TYPES_COLOR : Map<ZoneType, string> = new Map([
    [ZoneType.Available, "#00b450"],
    [ZoneType.Restricted, "#cc3b3b"],
    [ZoneType.Urban, "#FFD700"]
]);