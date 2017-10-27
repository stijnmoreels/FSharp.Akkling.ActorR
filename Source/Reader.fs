/// The Reader monad (also called the Environment monad).
/// Represents a computation, which can read values from a shared environment, pass values from function to function, and execute sub-computations in a modified environment.
module Reader

type Reader<'env, 'a> = Reader of ('env -> 'a)

/// Runs a Reader and extracts the final value from it.
/// ## Parameters
///  - `env` - An initial environment.
///  - `action` - Computation to be run in the environment.
let run env (Reader action) = 
    action env

/// Retrieves a function of the current environment.
/// ## Parameters
///  - `x` - Value to be lift inside the current environment
let retn x = Reader <| fun env -> x

/// Retrieves the 'Reader' monad environment.
/// ## Parameters
///  - `arg0` - The selector function to apply to the environment.
let ask = Reader

/// Unwraps the current value inside the environment to run a cross-world function against it within that environment.
/// ## Parameters
///  - `f` - A cross-world function to run against the value within the environment.
///  - `xAction` - Current value within the environment.
let bind f xAction = Reader <| fun env ->
    let x = run env xAction
    run env <| f x

let (>>=) x f = bind f x
let (=<<) = bind

let (>=>) r1 r2 = r1 >> bind r2
let (<=<) r1 r2 = r2 >> bind r1
