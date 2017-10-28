# Message Splitter
The _Message Splitter_ pattern emits the incoming by decomposing the message into multiple ones.

![Splitter](http://www.enterpriseintegrationpatterns.com/img/Sequencer.gif)

Here's the implementation I provided:

```fsharp
//// Actor Splitter: decompose the incoming message into multiple ones before sending it to the next Actor.
/// ## Parameters
///  - `f` - A function to decompose the incoming message in a series of messages.
let split f next = askSysOf <| fun m ->
    Actor.tellAll (f m) next
    |> ignored
```

We could test this if we let *FsCheck* decompose the message:

```fsharp
[<Property>]
let ``Message gets split into multiple messages`` (m : int) (f : int -> int list) =
    testDefault <| fun tck ->
        ActorR.split f
        =<< ActorR.spy' ()
        |> Reader.run tck
        |> Actor.tell m

        expectMsgAllOf tck (f m) |> ignore
```

We can of course extend this example by also making changing the target destination for each decomposed message.
This would require us to look-up the target _Actor_.

Like this:

```fsharp
/// Actor Splitter: routes incoming message based on their content to a destination.
/// ## Parameters
///  - `lookup` - A function to look up the destination based on the incoming message.
///  - `f` - A function to decompose the incoming message into multiple ones.
let splitRoute f lookup = askSysOf2 <| fun (ctx : Actor<_>) m ->
    for x in f m do 
        lookup x 
        |> run ctx.System
        |> Actor.tell x
    |> ignored
```

Which we again could test with the approach:

```fsharp
[<Property>]
let ``Message gets split before sending to the looked-up Actor`` (m : int) (f : int -> int list) =
    testDefault <| fun tck ->
        ActorR.splitRoute f (fun _ -> ActorR.spy tck)
        |> Reader.run tck.Sys
        |> Actor.tell m

        expectMsgAllOf tck (f m) |> ignore
```
    
