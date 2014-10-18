﻿namespace BookLibrary

open System

open FSharpx
open Eventful
open Eventful.AggregateActionBuilder

type AggregateType =
| Book
| BookCopy

type EmergencyEventMetadata = {
    MessageId: Guid
    AggregateId : Guid
    SourceMessageId: string
    EventTime : DateTime
}

module Aggregates = 
    let systemConfiguration = { 
        SetSourceMessageId = (fun id metadata -> { metadata with SourceMessageId = id })
        SetMessageId = (fun id metadata -> { metadata with MessageId = id })
    }

    let stateBuilder<'TId when 'TId : equality> = UnitStateBuilder.nullUnitStateBuilder<EmergencyEventMetadata, 'TId>

    let emptyMetadata aggregateId messageId sourceMessageId = { SourceMessageId = sourceMessageId; MessageId = messageId; EventTime = DateTime.UtcNow; AggregateId = aggregateId }

    let inline simpleHandler f = 
        let withMetadata = f >> (fun x -> (x, emptyMetadata))
        Eventful.AggregateActionBuilder.simpleHandler systemConfiguration stateBuilder withMetadata
    
    let inline buildCmdHandler f =
        f
        |> simpleHandler
        |> buildCmd

    let inline fullHandler s f =
        let withMetadata a b c =
            f a b c
            |> Choice.map (fun evts ->
                evts 
                |> List.map (fun x -> (x, emptyMetadata))
                |> List.toSeq
            )
        Eventful.AggregateActionBuilder.fullHandler systemConfiguration s withMetadata

    let toAggregateDefinition = Eventful.Aggregate.toAggregateDefinition