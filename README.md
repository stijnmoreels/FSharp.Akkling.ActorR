# FSharp.Akkling.ActorR
Extra Actor functionality on top of the existing Akkling repository to create an Actor pipeline with ease.

## ActorR Module
What frustrated me the most; was that _Actors_ are not "by default" composable like we're used with functions. If you want to send a message from one **Actor** to another **Actor**, you must send it's reference (from the receiver) to the sender Actor.
Especially if you want to set up a pipeline, this could be a problem for readability.

One of my attempts was to create an higher-level language in which we can express functions in terms of **Actors**; that's what the ```ActorR``` module is all about.

Here's all the different _Integration Patterns_ I tried to implement with _Akka.NET_ Actors (and the F# _Akkling_ extension):

* [Content Enricher/Message Translater](Pages/Map.md)
* [Idempotent Receiver](Pages/Dedup.md)
* [Wire Tap](Pages/Tap.md)
* [Routing Switch](Pages/Switch.md)
* [Splitter](Pages/Splitter.md)
* [Aggregator](Pages/Aggregator.md)
* [Resequencer](Pages/Reseq.md)

## Reader Module
All the functions exposed in the ```ActorR``` uses the _Reader Monad_ to have a latter-initialization of the actual Actor system; so we can specify the actual system at the very last with ```Reader.run sys```.

## Integration Patterns
Now that you have an introduction into the two major modules, I'll show you the combinations of these systems.

### Introduction with Filter
One of the frequently used _Integration Patterns_, is the _Filter Pattern_. This is the implementation of this filter:

```fsharp
/// Actor Filter: filters incoming message before sending it to the next Actor.
/// ## Parameters
///  - `f` - A predicate to filter the incoming message for the next Actor.
///  - `next` - The next Actor in line to handle the message.
let filter f next = askSysOf <| function
    | x  when f x -> next <! x |> ignored
    | _ -> unhandled ()
```
    
Just for your information, the ```askSysOf``` is a helper function that lets me use the _Reader_ functionality to ask for a Actor system:

```fsharp
let private askSys a = Reader.ask <| fun sys ->
    spawnAnonymous sys <| props a

let private askSysOf fn = askSys <| actorOf fn
```

Now let's write a test to verify this so you can see it in practice:

```fsharp
let expect tck pred m =
    match pred with
    | true -> expectMsg tck m |> ignore
    | false -> expectNoMsg |> ignore

[<Property>]
let ``Message gets passed by Actor if filter satisfy condition`` 
    (retn : bool) 
    (i : int) =
    testDefault <| fun tck ->
        ActorR.filter (fun _ -> retn)
        =<< ActorR.spy' ()
        |> Reader.run tck
        |> Actor.tell i

        expect tck retn i |> ignore
```

- The ```ActorR.spy ()``` call is just a creation function for the _TestKit TestActor_ but in a ```ActorR``` context.
- The ```Actor.tell``` call is a alias for the ```<!``` infix operator.

You can see that we reversed bindings (```=<<```), I've created a pipeline in which the order of execution is from top to bottom (and not the way arround).
