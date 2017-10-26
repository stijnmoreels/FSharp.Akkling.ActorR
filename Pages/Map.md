# Map Pattern
A.k.a. _Content Enricher_, _Message Translater_, ... or any other _Integration Pattern_ that allows you to modify the outcome message that is send in the pipeline.
(immutuable of course).

![Message Translater](http://www.enterpriseintegrationpatterns.com/img/MessageTranslator.gif)

This is the implementation:

```fsharp
/// Actor Map: maps the incoming message before sending it to the next Actor.
/// ## Parameters
///  - `f` - A function that maps the the incoming message.
///  - `next` - The next Actor in line to handle the message
let map f next = askSysOf <| fun x -> 
    next <! (f x)  |> ignored
```

We just run a function to the incoming message before sending it to the next Actor.
We can easily test this using _FsCheck_:

```fsharp
[<Property>]
let ``Message gets mapped by Actor with given function`` (i : int) (f : int -> int) =
    testDefault <| fun tck ->
        ActorR.map f
        =<< ActorR.spy ()
        |> Reader.run tck
        |> Actor.tell i

        expectMsg tck (f i) |> ignore
```
