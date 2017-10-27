# Routing Switch
One of the patterns to balance load, is to switch between multiple _Actors_. 
For now, I've created a _Switch_ that switches between two actors, this could be extended of course to more than two.

Here's the implementation:

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
