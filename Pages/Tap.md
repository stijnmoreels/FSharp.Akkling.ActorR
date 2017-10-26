# Wire Tap Pattern
We can use a _Wire Tap_ to inspect the pipelined-message or to execute an _side-effect_ function within our pipeline.

![Wire Tap](http://www.enterpriseintegrationpatterns.com/img/WireTap.gif)

Here's the implementation:

```fsharp
/// Actor Wire Tap: inspects but not alter the incoming message before sending the orignal to the next Actor.
/// ## Parameters
///  - `f` - A dead-end function to inspect the incoming message.
///  - `next` - The next Actor in line to handle the message.
let tap f next = askSysOf <| fun x -> 
    f x; next <! x |> ignored
```

Nothing fancy.
I used a ```mutable``` variable in order to test this _side-effect_ function:

```fsharp
[<Property>]
let ``Message gets inspected by the tap function`` (NonZeroInt expected) =
    testDefault <| fun tck ->
        let mutable actual = 0
        ActorR.tap (fun m -> actual <- m)
        =<< ActorR.spy ()
        |> Reader.run tck
        |> Actor.tell expected

        expectMsg tck expected |> ignore
        assert (expected = actual)
```
