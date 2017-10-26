# Request-Reply Pattern
To reply to an _Actor_, we can use the ```ActorR.reply``` functionality. 
It sends a response for a given request back to the sender of the message.

![Request-Reply](http://www.enterpriseintegrationpatterns.com/img/RequestReply.gif)

Here's the implementation:

```fsharp
/// Actor Reply: replies to the Actor Sender by running a function to the incoming message.
/// ## Parameters
///  - `f` - A function to run on the incoming message before sending it to the Actor Sender.
let reply f = askSysOf2 <| fun (ctx : Actor<_>) x ->
        ctx.Sender () <! f x |> ignored
```
