# FSharp.Akkling.ActorR
Extra Actor functionality on top of the existing Akkling repository to create an Actor pipeline with ease.

## ActorR Module
What frustrated me the most; was that _Actors_ are not "by default" composable like we're used with functions. If you want to send a message from one **Actor** to another **Actor**, you must send it's reference (from the receiver) to the sender Actor.
Especially if you want to set up a pipeline, this could be a problem for readability.

One of my attempts was to create an higher-level language in which we can express functions in terms of **Actors**; that's what the ```ActorR``` module is all about.

## Reader Module
All the functions exposed in the ```ActorR``` uses the ```Reader Monad``` to have a latter-initialization of the actual Actor system; so we can specify the actual system at the very last with ```Reader.run sys```.

## Integration Patterns
Now that you have an introduction into the two major modules, I'll show you the combinations of these systems.
