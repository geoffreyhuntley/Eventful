﻿namespace Eventful.Testing

open System
open Eventful
open FSharpx.Collections
open FSharpx.Choice
open FSharpx.Option
open Eventful.EventStream

type TestSystem<'TMetadata, 'TCommandContext, 'TBaseEvent when 'TMetadata : equality>
    (
        handlers : EventfulHandlers<'TCommandContext,unit,'TMetadata, 'TBaseEvent>, 
        lastResult : CommandResult<'TBaseEvent,'TMetadata>, 
        allEvents : TestEventStore<'TMetadata>
    ) =

    let interpret prog (testEventStore : TestEventStore<'TMetadata>) =
        TestInterpreter.interpret 
            prog 
            testEventStore 
            handlers.EventStoreTypeToClassMap 
            handlers.ClassToEventStoreTypeMap
            Map.empty 
            Vector.empty

    member x.RunCommandNoThrow (cmd : obj) (context : 'TCommandContext) =    
        let cmdType = cmd.GetType()
        let cmdTypeFullName = cmd.GetType().FullName
        let handler = 
            handlers.CommandHandlers
            |> Map.tryFind cmdTypeFullName
            |> function
            | Some (EventfulCommandHandler(_, handler,_)) -> handler context
            | None -> failwith <| sprintf "Could not find handler for %A" cmdType

        let (allEvents, result) = TestEventStore.runCommand interpret cmd handler allEvents

        let allEvents = TestEventStore.processPendingEvents () interpret handlers allEvents

        new TestSystem<'TMetadata, 'TCommandContext, 'TBaseEvent>(handlers, result, allEvents)

    // runs the command. throws on failure
    member x.RunCommand (cmd : obj) (context : 'TCommandContext) =    
        let system = x.RunCommandNoThrow cmd context
        match system.LastResult with
        | Choice1Of2 _ ->
            system
        | Choice2Of2 e ->
            failwith <| sprintf "Command failed %A" e

    member x.Handlers = handlers

    member x.LastResult = lastResult

    member x.Run (cmds : (obj * 'TCommandContext) list) =
        cmds
        |> List.fold (fun (s:TestSystem<'TMetadata, 'TCommandContext, 'TBaseEvent>) (cmd, context) -> s.RunCommand cmd context) x

    member x.EvaluateState (stream : string) (identity : 'TKey) (stateBuilder : IStateBuilder<'TState, 'TMetadata, 'TKey>) =
        let streamEvents = 
            allEvents.Events 
            |> Map.tryFind stream
            |> function
            | Some events -> 
                events
            | None -> Vector.empty

        let run s (evt : obj, metadata) : Map<string,obj> = 
            AggregateStateBuilder.dynamicRun stateBuilder.GetBlockBuilders identity evt metadata s
        
        streamEvents
        |> Vector.map (function
            | (position, Event { Body = body; Metadata = metadata }) ->
                (body, metadata)
            | (position, EventLink (streamId, eventNumber, _)) ->
                allEvents.Events
                |> Map.find streamId
                |> Vector.nth eventNumber
                |> (function
                        | (_, Event { Body = body; Metadata = metadata }) -> (body, metadata)
                        | _ -> failwith ("found link to a link")))
        |> Vector.fold run Map.empty
        |> stateBuilder.GetState

    static member Empty handlers =
        let emptySuccess = {
            CommandSuccess.Events = List.empty
            Position = None
        }
        new TestSystem<'TMetadata, 'TCommandContext, 'TBaseEvent>(handlers, Choice1Of2 emptySuccess, TestEventStore.empty)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestSystem = 
    let runCommand x c (y:TestSystem<'TMetadata, 'TCommandContext, 'TBaseEvent>) = y.RunCommand x c
    let runCommandNoThrow x c (y:TestSystem<'TMetadata, 'TCommandContext, 'TBaseEvent>) = y.RunCommandNoThrow x c