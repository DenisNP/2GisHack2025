export interface ApiPoint {
    lat: number;
    lon: number;
}

export interface ApiRubric {
    id: string;
    parent_id?: string;
}

export interface ApiEntranceGeometry {
    points: string[];
}

export interface ApiDatabaseEntrance {
    geometry: ApiEntranceGeometry;
}

export interface ApiItemGeometry {
    hover?: string;
}

export interface ApiItemLinks {
    database_entrances?: ApiDatabaseEntrance[];
    entrances?: ApiDatabaseEntrance[];
}

export interface ApiItem {
    geometry?: ApiItemGeometry;
    links?: ApiItemLinks;
    point: ApiPoint;
    type: string;
    rubrics?: ApiRubric[];
}

export interface ApiResult {
    items: ApiItem[];
    total: number;
}

export interface ApiResponse {
    meta: {
        api_version: string;
        code: number;
        issue_date: string;
    };
    result: ApiResult;
}

