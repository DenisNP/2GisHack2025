import { GeoPoint } from "../../types/GeoPoint";

export type SidewalkData = {
    id: number;
    centerLine: GeoPoint[];
    polygon: GeoPoint[];
    width: number;
};

export type UrbanDrawerProps = {
    width?: number; // ширина тротуара в метрах (по умолчанию 2)
    color?: string; // цвет тротуара (по умолчанию желтый)
    label?: string;
    sidewalks?: SidewalkData[]; // контролируемый список тротуаров
    onSidewalksChanged?: (sidewalks: SidewalkData[]) => void;
    position?: { left?: number; top?: number };
}