import { createEffect, createEvent, createStore, sample } from "effector";

/*events*/
const click = createEvent();
const resetCounter = createEvent();
const calc = createEvent()

/*effects*/
const clickFx = createEffect(async ()=>{
    ///work
})

const calcFx = createEffect((count:number)=>{
    console.log('calcFx', count)
    return 10;
});

/*stores*/
const $countClick = createStore(0).reset([resetCounter])
$countClick.on(click, (state) => state + 1);
$countClick.on(calcFx.done, (state, {result}) => state + result);

/*bindings */
sample({
    clock: click,
    target: clickFx
})

sample({
    clock: calc,
    source: $countClick,
    target: calcFx
})


export const events = {
    click,
    calc,
}

export const stores = {
    $countClick
}