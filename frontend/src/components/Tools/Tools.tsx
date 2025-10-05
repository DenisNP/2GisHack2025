import { Button, Divider, Stack } from "@mui/material";
import React from "react";
import { useUnit } from 'effector-react';
import { events } from '../../stores/globalState';
import { StateManager } from "./components/StateManager";

export const Tools: React.FC = () =>{
    const getJson = useUnit(events.getJson)
    

    return <Stack spacing={1}>
         <Button 
            onClick={getJson}
            variant="contained"
            fullWidth
        >
            GET JSON
        </Button>
        <Divider/>
        <StateManager />
    </Stack>
}