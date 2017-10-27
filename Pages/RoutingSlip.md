# Routing Slip
With the _Routing Slip_ we can change to order of the message processing at run-time. 
We don't know at design-time in which order the message must be processed; so we need some kind of registration for this run-time-pipeline.

Here's the implementation:

```fsharp
type Envelope<'a, 'b> = 
    { Message : 'a
      Routing : 'b list
      History : 'b list }
      
let private askSys a = ask <| fun sys ->
    spawnAnonymous sys <| props a
      
let private askSysOf2 fn = askSys <| actorOf2 fn

/// Actor Routing Slip: sets up a 'Routing Slip' for the incoming message by looking up the next processor.
/// ## Parameters
///  - `lookup` - A function to look-up the next processor in line for a given route.
///  - `next` - The next Actor in line to handle the message after the routing-slip is finished.
let slip lookup next = askSysOf2 <| fun (ctx: Actor<_>) e ->
    match e.Routing with
    | next :: rest -> 
        run ctx.System (lookup next)
        <! { e with Routing = rest; 
                    History = next :: e.History }
        |> ignored
    | _ -> next <! e |> ignored
```

Not very much, is it?
I've created a new type of message that needs to be passed in to the _Routing Slip_ Actor, called an ```Envelope```.
This type contains the _to-be-processed_ order an the _history_ of the message (a.k.a. _Envelope_).
I've also added a field called ```Message``` to define your own custom *Message Body*.

Now, the ```slip``` function needs a ```lookup``` function that's used to determine the next message processor in the pipeline.

Here's how you would test it:

```fsharp
type Route = A | B | C

[<Property>]
let ``Routing Slip is being used as route`` (m : int) (list : Route list) =
    testDefault <| fun tck ->        
        ActorR.slip (fun _ -> ActorR.echo tck)
        =<< ActorR.spy ()
        |> Reader.run tck
        |> Actor.tell { Routing = list; History = []; Message = m }

        list 
        |> List.mapi (fun i _ -> 
            let index = i + 1
            { Routing = list |> List.skip index
              History = list |> List.take index |> List.rev
              Message = m })
        |> expectMsgAllOf tck
        |> ignore
```
