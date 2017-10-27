/// Actor Pipes and Filters extensions to set up a Actor-Pipeline.
module ActorR

open Akka
open Akkling
open Akkling.Actors
open Reader
open Envelope
open System
open Akka.Actor

let private askSys a = ask <| fun sys ->
    spawnAnonymous sys <| props a

let private askSysOf fn = askSys <| actorOf fn
let private askSysOf2 fn = askSys <| actorOf2 fn

let (!>) x a = a <! x

/// Actor Filter: filters incoming message before sending it to the next Actor.
/// ## Parameters
///  - `f` - A predicate to filter the incoming message for the next Actor.
///  - `next` - The next Actor in line to handle the message.
let filter f next = askSysOf <| function
    | x  when f x -> next <! x |> ignored
    | _ -> unhandled ()

/// Actor Map: maps the incoming message before sending it to the next Actor.
/// ## Parameters
///  - `f` - A function that maps the the incoming message.
///  - `next` - The next Actor in line to handle the message
let map f next = askSysOf <| fun x -> 
    next <! (f x)  |> ignored

/// Actor Wire Tap: inspects but not alter the incoming message before sending the orignal to the next Actor.
/// ## Parameters
///  - `f` - A dead-end function to inspect the incoming message.
///  - `next` - The next Actor in line to handle the message.
let tap f next = askSysOf <| fun x -> 
    f x; next <! x |> ignored

/// Actor Detour: routes the message to a series of addiontional steps if the condition is met.
/// ## Parameters
///  - `f` - A predicate to identify if incoming message must visit the detour first.
///  - `extra` - A set of addiontional steps where the incoming message will be routed during the detour.
///  - `next` - The next Actor in line to handle the message.
let detour f extra next = askSysOf2 <| fun (ctx : Actor<_>) x ->
    match x with
    | m when f m -> 
        (run ctx.System extra <? m) 
        |> pipeTo ctx.UntypedContext.Self next
        |> ignored
    | m -> next <! m |> ignored

/// Actor Spy: creates a 'TestKit' Actor.
let spy () = ask <| fun sys ->
    Akkling.TestKit.testActor sys <| 
        sprintf "spy-actor-%A" (System.Guid.NewGuid ())

/// Actor Reply: replies to the Actor Sender by running a function to the incoming message.
/// ## Parameters
///  - `f` - A function to run on the incoming message before sending it to the Actor Sender.
let reply f = askSysOf2 <| fun (ctx : Actor<_>) x ->
        ctx.Sender () <! f x |> ignored

/// Actor Deduplicator: makes sure that only unique incoming messages are send the the next Actor
/// ## Parameters
///  - `next` - The next Actor in line to handle the message.
let dedup contains next = 
    let rec fn contains next stored = function
        | m when stored |> contains m -> ignored ()
        | m -> next <! m
               become <| fn contains next (m :: stored)

    askSysOf <| fn contains next []

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

let private gatherChildProps f =
    props (actorOf <| fun (finish, id, x) -> 
        finish <! Gather (id, f x) |> stop)

let private scatterGather<'a> next child (ctx : Actor<_>) =
    let scatter reg xs =
        let key = Guid.NewGuid ()
        for x in xs do spawnAnonymous ctx child <! (ctx.Self, key, x)
        Map.add key (List.length xs, []) reg

    let gather reg (key, x) =
        match Map.tryFind key reg with
        | Some (l, xs) -> 
            match x :: xs with
            | xxs when List.length xxs = l -> 
                next <! xxs
                Map.remove key reg
            | xxs -> Map.add key (l, xxs) reg
        | None -> reg

    let rec loop reg = actor {
        let! msg = ctx.Receive ()
        return! loop <|
            match msg with
            | Scatter xs -> scatter reg xs
            | Gather (key, x) -> 
                ctx.Sender () <! PoisonPill.Instance
                gather reg (key, x)
    }
    loop Map.empty<Guid, int * 'a list>

let scatter f next = askSys <| scatterGather next (gatherChildProps f)

/// Actor Port: in the 'Ports and Adapters', this is a outbound-port in chain of Actors
/// ## Parameters
///  - `f` - A function to execute on the end result at the end of the the chain of Actors.
let port f = askSysOf <| fun x -> f x; ignored ()

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
    
/// Actor Splitter: routes incoming message based on their content to a destination.
/// ## Parameters
///  - `lookup` - A function to look up the destination based on the incoming message.
let split lookup f = askSysOf2 <| fun (ctx : Actor<_>) x ->
    let xs = f x
    for x in xs do
       x |> (lookup >> run ctx.System) <! x
    ignored ()

/// Actor Supervision: supervise another Actor (or Actors) with a given strategy.
/// # Parameters
/// - `child` - A child Actor for which the supervision strategy must be set.
/// - `strategy` - A supervision strategy to use when the child Actor fails.
let supervise child strategy = ask <| fun sys ->
    let fn (ctx : Actor<_>) =
        let passThrough = run ctx child
        
        let rec loop () = actor {
            let! msg = ctx.Receive ()
            passThrough <! msg
            return! loop () }
        loop ()

    spawnAnonymous sys { props fn with SupervisionStrategy = Some strategy }
