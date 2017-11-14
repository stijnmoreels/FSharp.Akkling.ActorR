# Scatter-Gather Pattern

The _Scatter-Gather_ pattern is a combination of both the _Splitter_ and the _Aggregator_. So that's what we're going to do.

![Scatter-Gather](http://www.enterpriseintegrationpatterns.com/img/BroadcastAggregate.gif)

The implementation is a combination of both patterns which we can test with this:

```fsharp
let waitTillAll f expect actual =
    if expect = actual 
    then Some (f expect, [])
    else None

[<Property>]
let ``Message gets Scattered and Gatherered by combination`` 
    (m : int)
    (f : int -> int list)
    (g : int list -> int) =
    f m <> [] ==> lazy
    testDefault <| fun tck ->
        ActorR.split (f >> List.rev)
        <=< ActorR.aggregate (waitTillAll g (f m))
        =<< ActorR.spy' ()
        |> Reader.run tck
        |> Actor.tell m

        expectMsg tck (f >> g <| m) |> ignore
```

We ask _FsCheck_ for functions that explode and implote a message (in this case, just an ```int```); 
which we send with the ```agregate``` and the ```split``` functions of the ```ActorR``` module.
