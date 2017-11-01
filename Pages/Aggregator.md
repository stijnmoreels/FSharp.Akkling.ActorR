# Aggregator Pattern
When a series of message arrives that needs to be wrapped into one, we can use the _Aggregator Pattern_.
To know which messages belongs together, we can use some correlation ID, or just batch them together when the required number of messages arrives.

Here's the implementation I implemented:

```fsharp
/// Actor Aggregator: combine multiple incoming messages into one before sending it to the next Actor.
/// ## Parameters
///  - `f` - A function to determine if the required amount of messages has arrived and so, can be send to the next Actor.
let aggregate f next =
    let rec fn queue m =
        match f (m :: queue) with
        | None -> become <| fn (m :: queue)
        | Some (x, rest) -> 
            next <! x
            become <| fn rest
    
    askSysOf <| fn []
```

Here's the test I wrote for this pattern:

```fsharp
[<Property>]
let ``Message gets aggregated into a single message`` 
    (xs : int list) 
    (f : int list -> int) =
    xs <> [] ==> lazy
    testDefault <| fun tck ->
        ActorR.aggregate (fun queue -> 
            if queue = xs
            then Some (f queue, [])
            else None)
        =<< ActorR.spy' ()
        |> Reader.run tck
        |> Actor.tellAll (xs |> List.rev)

        expectMsg tck (f xs) |> ignore
```

We let _FsCheck_ decide how we should combine the messages; the test now only checks if all the messages has arrived before combining them.
