﻿namespace Eventful

type accumulator<'TState,'TItem> = 'TState -> 'TItem -> 'TState

type StateBuilder<'TState>(zero : 'TState, handlers : List<('TState -> obj -> 'TState)>) = 
    member x.Zero = zero
    member x.AddHandler<'T> (f:accumulator<'TState, 'T>) =
        let func (state : 'TState) (message : obj) =
            match message with
            | :? 'T as msg -> f state msg
            | _ -> state

        let msgType = typeof<'T>
        new StateBuilder<'TState>(zero, func::handlers)
        
    member x.Run (state: 'TState) (item: obj) =
        handlers
        |> List.fold (fun s h -> h s item) state

    static member Combine<'TState1, 'TState2, 'TState> (builder1 : StateBuilder<'TState1>) (builder2 : StateBuilder<'TState2>) combiner extractor =
       let zero = combiner builder1.Zero builder2.Zero 
       let handler (state : 'TState) (message : obj) = 
            let (state1, state2) = extractor state
            let state1' = builder1.Run state1 message
            let state2' = builder2.Run state2 message
            combiner state1' state2'
       new StateBuilder<'TState>(zero, [handler])
    static member Empty zero = new StateBuilder<'TState>(zero, List.empty)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StateBuilder =
    let Counter<'T> = 
        let s = StateBuilder.Empty 0
        s.AddHandler (fun c (x : 'T) -> c + 1)

    let SetBuilder<'T, 'TAddMessage, 'TRemoveMessage when 'T : comparison> (addItem : 'TAddMessage -> 'T) (removeItem: 'TRemoveMessage -> 'T)  =
        let s = StateBuilder.Empty (Set.empty : Set<'T>)
        let s = s.AddHandler((fun (x : Set<'T>) (add : 'TAddMessage) -> x |> Set.add (addItem add)))
        let s = s.AddHandler((fun (x : Set<'T>) (remove : 'TRemoveMessage) -> x |> Set.remove (removeItem remove)))
        s

    let lastValue<'TState, 'TEvent> (f : 'TEvent -> 'TState) (zero : 'TState) = 
        let s = StateBuilder.Empty zero
        s.AddHandler (fun _ m -> f m)

    let mapMessages<'TState,'TInputEvent, 'TOutputEvent> (f : 'TInputEvent -> 'TOutputEvent) (sb : StateBuilder<'TState>)=
        let sb' = StateBuilder.Empty sb.Zero
        let mappingAccumulator s i =
            let outputValue = f i
            sb.Run s (outputValue :> obj)
        sb'.AddHandler<'TInputEvent> mappingAccumulator

    let addHandler<'T, 'TEvent> (f : accumulator<'T,'TEvent>) (b : StateBuilder<'T>) =
        b.AddHandler f

    let mapMandler<'TMsg,'TKey,'TState when 'TKey : comparison> (getKey : 'TMsg -> 'TKey) (accumulator : accumulator<'TState,'TMsg>) (zero : 'TState) : accumulator<Map<'TKey,'TState>,'TMsg> =
        let acc (state : Map<'TKey,'TState>) msg =
            let key = getKey msg
            let childState = 
                if state |> Map.containsKey key then
                    state |> Map.find key
                else
                    zero

            let childState' = accumulator childState msg
            state |> Map.add key childState'
        acc