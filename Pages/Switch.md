# Routing Switch
One of the patterns to balance load, is to switch between multiple _Actors_. 
For now, I've created a _Switch_ that switches between two actors, this could be extended of course to more than two.

Here's the implementation:

```fsharp
/// Actor Switch: incoming messages will be routed to two destinations switching from left to right with every incoming message.
/// ## Parameters
///  - `left` - The left destination to where the incoming message will be routed.
///  - `right` - The right destination to where the incoming messages will be routed.
let rec switch leftR rightR = askSys <| fun ctx ->
    let leftA = run ctx leftR
    let rightA = run ctx rightR

    let rec switchFunc left right = actor {
        let! x = ctx.Receive ()
        left <! x
        return! switchFunc right left }

    switchFunc leftA rightA
```

And here's how I would test this:

```fsharp
[<Property>]
let ``Message gets switched only to the left Actor`` 
    (f : int -> int) 
    (g : int -> int) 
    (ms : int list) =
    testDefault <| fun tck ->
        let map_spy f = ActorR.map f =<< ActorR.spy tck
        ActorR.switch (map_spy f) (map_spy g)
        |> Reader.run tck.Sys
        |> Actor.tellAll ms

        List.foldBack (fun x (ys, xs) -> (x :: xs, ys)) ms ([], [])
        |> Tuple.map2 (List.map f) (List.map g)
        |> Tuple.collapse List.append
        |> expectMsgAllOf tck
```

Of course, I added some helper functions to work with tuples:

```fsharp
module Tuple

let map2 f g (x, y) = f x, g y
let collapse f (x, y) = f x y
```

The point in the assertion, is to make a list for which all the even indexes gets mapped by the ```f``` function and all the
odd indexes gets mapped by the ```g``` function.
The result of this computation should be a list that contains all the values that's being emitted.
