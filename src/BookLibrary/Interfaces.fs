﻿namespace BookLibrary

open System.Threading.Tasks
open Eventful
open Eventful.EventStore
open System

type IBookLibrarySystem =
    abstract member RunCommand<'a> : 'a -> Async<CommandResult<IEvent,BookLibraryEventMetadata>>
    abstract member RunCommandTask<'a> : 'a -> Task<CommandResult<IEvent,BookLibraryEventMetadata>>

type BookLibraryEventStoreSystem = EventStoreSystem<unit,BookLibraryEventContext,BookLibraryEventMetadata,IEvent,AggregateType>

[<CLIMutable>]
type BookId = {
    Id : Guid
}
with static member New () = { Id = Guid.NewGuid() }

[<CLIMutable>]
type BookCopyId = {
    Id : Guid
}
with static member New () = { Id = Guid.NewGuid() }

[<CLIMutable>]
type AwardId = {
    Id : Guid
}
with static member New () = { Id = Guid.NewGuid() }

[<CLIMutable>]
type DeliveryId = {
    Id : Guid
}
with static member New () = { Id = Guid.NewGuid() }

[<CLIMutable>]
type FileId = {
    Id : Guid
}
with static member New () = { Id = Guid.NewGuid() }

type IBookEvent = 
    inherit IEvent
    abstract member BookId : BookId

[<CLIMutable>]
type BookCopyAddedEvent = 
    { BookCopyId : BookCopyId
      BookId : BookId }
    interface IBookEvent with
      member x.BookId = x.BookId

[<CLIMutable>]
type BookPromotedEvent = {
    BookId : BookId
}
with interface IEvent

[<CLIMutable>]
type BookPrizeAwardedEvent = {
    AwardId : AwardId
    BookId : BookId
}
with interface IEvent

[<CLIMutable>]
type DeliveryAcceptedEvent = {
    DeliveryId : DeliveryId
    FileId : FileId
}
with interface IEvent
