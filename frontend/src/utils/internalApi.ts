import { RunSimulationRequest, RezultPointResponse } from "../types/InternalApi";
import { convertToSnakeCase } from "./convertToSnakeCase";

const RUN_SIMULATION = "/runSimulation"
const SERVER_URL = process.env.REACT_APP_SERVER_URL || ''


export const runSimulationApi = async (request: RunSimulationRequest): Promise<RezultPointResponse[]> => {
    try {
        const response = await fetch(`${SERVER_URL}${RUN_SIMULATION}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: convertToSnakeCase(JSON.stringify(request, null, 2))
        });

        if (!response.ok) {
            throw new Error("Network response was not ok");
        }

        const data = await response.json();
        return data;
    } catch (error) {
        console.error("Error occurred while running simulation:", error);
        throw error;
    }
}