# Resequencer Pattern
When messages arrive in not the expected order; we can _Resequence_ them with this pattern.
We need to know which message to start with, what eacht next message will look like and how to sort the message that are "on-hold" so we can loop over the left-over messages.

Here's the implementation:

```fsharp
/// Actor Resequencer: resequence incoming messages in the right order before sending it to the next Actor.
/// ## Parameters
///  - `init` - Initial message to start looking for.
///  - `fSort` - A function to sort the already queued messages.
///  - `fWhatsNext` - A function to determine the next message when giving the previous message.
///  - `nextA` - The next Actor that gets the incoming messages in order.
let reseq init fSort fWhatsNext nextA =
    let folder m (next, rest) =
        if m <> next
        then next, rest
        else nextA <! m
             fWhatsNext m, rest |> List.except [m]

    let rec fn queue current x =
        let updated = x :: queue
        updated
        |> List.sortWith fSort
        |> List.rev
        |> flip (List.foldBack folder) (current, updated)
        |> fun (next, rest) -> become <| fn rest next
    
    askSysOf <| fn [] init
```

And here's how I would test this:

```fsharp
let compareTo (x : int) y = x.CompareTo y
let add1 = (+) 1

let waitForAll expect actual =
    if (List.length actual) = (List.length expect)
    then Some (actual, [])
    else None

[<Property>]
let ``Message gets resequenced in the right order`` (length : PositiveInt) =
    [1..length.Get]
    |> Gen.shuffle
    |> Gen.map Array.toList
    |> Arb.fromGen
    |> Prop.forAll <| fun expect ->
        testDefault <| fun tck ->
            ActorR.reseq 1 compareTo add1
            <=< ActorR.aggregate (waitForAll expect)
            =<< ActorR.spy' ()
            |> Reader.run tck
            |> Actor.tellAll expect

            expectMsg tck (expect |> List.sort |> List.rev) |> ignore
```

It's a possible solution to check if the message arrive correctly if we use also the _Aggregator_ to combine all the messages before sending it further.
