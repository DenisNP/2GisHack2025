import { Divider, Stack } from "@mui/material";
import React from "react";
import { StateManager } from "./components/StateManager";
import { Simulator } from "./components/Simulator";

export const Tools: React.FC = () =>{
    return <Stack spacing={1}>
        <Simulator/>
        <Divider/>
        <StateManager />
    </Stack>
}