# Idempotent Receiver Pattern
Only allow unique message to pass through. That's what we need. 
In order to specify if a message is unique or not, we can specify a function to verify this.

Here's the implementation:

```fsharp
/// Actor Deduplicator: makes sure that only unique incoming messages are send the the next Actor
/// ## Parameters
///  - `next` - The next Actor in line to handle the message.
let dedup contains next = 
    let rec fn contains next stored = function
        | m when stored |> contains m -> ignored ()
        | m -> next <! m
               become <| fn contains next (m :: stored)

    askSysOf <| fn contains next []
```

The ```contains``` function will be called to check if the incoming message is already stored or not.

You could test this with something like this:

```fsharp
[<Property>]
let ``Message gets skipped when stored`` (m : int) =
        testDefault <| fun tck ->
            ActorR.dedup (fun _ _ -> true)
            =<< ActorR.spy ()
            |> Reader.run tck
            |> Actor.tell m

            expectNoMsg |> ignore

[<Property>]
let ``Message gets passed when unique`` (m : int) =
    testDefault <| fun tck ->
        ActorR.dedup (fun _ _ -> false)
        =<< ActorR.spy ()
        |> Reader.run tck
        |> Actor.tell m

        expectMsg tck m |> ignore
```

Or combined:

```fsharp
[<Property>]
let ``Message skipped when already stored`` (m : int) (PositiveInt times) =
        testDefault <| fun tck ->
            let a = ActorR.dedup List.contains
                    =<< ActorR.spy ()
                    |> Reader.run tck

            a |> Actor.tell m
            expectMsg tck m |> ignore

            Seq.replicate times m
            |> Seq.iter (flip Actor.tell a)
            expectNoMsg |> ignore
```
