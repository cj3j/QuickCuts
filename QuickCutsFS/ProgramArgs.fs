// QuickCuts Copyright (c) 2017 C. Jared Cone jared.cone@gmail.com
//
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.

namespace QuickCutsFS

open System

module ProgramArgs =

    type ProgramArguments = {
        file: string;
        profile: string;
        list: bool
        command: string;
        args: string list;
    }
    with static member New = { file = "Commands.txt"; profile = "Default"; command = ""; list = false; args = []; }

    module Exceptions = 
        type ArgMissingValue(arg:string) =
            inherit Exception(sprintf "Argument '%s' requires a value." arg)
            let arg = arg

        type ArgNotSupported(arg:string) =
            inherit Exception(sprintf "Argument '%s' is not supported." arg)
            let arg = arg

        type CommandNotSpecified() =
            inherit Exception("A command must be specified")

    let getUsage =
        sprintf "%s\n%s\n%s"
        <| "Usage: -file [path to commands file] -profile [name of profile] <-list> [command name] <command arguments>"
        <| "Example: -file \"c:\commands.txt\" -profile Default -list"
        <| "Example: -file \"c:\commands.txt\" -profile Default notepad"

    let parseArgs (argList:string list) =

        let (|Prefix|_|) prefix (str:string) =
            match str.StartsWith(prefix) with
            | true -> Some()
            | false -> None

        let parseArgValue key argList = 
            match argList with
            | value::rest ->
                match value with
                | Prefix "-" -> argList, (key, None)
                | _ -> rest, (key, Some value)
            | [] -> argList, (key, None)

        let parseArg key argList =
             match key with
                | Prefix "-" -> Some(parseArgValue key argList)
                | _ -> None

        let rec parseArgPairs argList pairs =
            match argList with
            | key::rest ->
                match parseArg key rest with
                | Some (list, pair) -> parseArgPairs list (pair::pairs)
                | None -> argList, pairs
            | [] -> argList, pairs

        let setArgValue args name optValue setValue =
            match optValue with
            | Some newValue ->
                let newArgs = setValue args newValue
                newArgs
            | None -> raise <| Exceptions.ArgMissingValue(name)

        let applyArg (name, value) args =
            match name with
            | "-file" -> setArgValue args name value (fun args newValue -> { args with file = newValue })
            | "-profile" -> setArgValue args name value (fun args newValue -> { args with profile = newValue })
            | "-list" -> { args with list = true }
            | _ -> raise <| Exceptions.ArgNotSupported(name)

        let rec applyArgs argPairs progArgs =
            match argPairs with
            | pair::rest ->
                applyArg pair progArgs
                |> applyArgs rest
            | [] -> progArgs

        let setCommand argList progArgs =
            match progArgs.list with
            | true -> progArgs
            | false ->
                match argList with
                | command::rest -> { progArgs with command = command; args = rest; }
                | [] -> raise <| Exceptions.CommandNotSpecified()

        let commandArgList, argPairs = parseArgPairs argList []

        applyArgs argPairs ProgramArguments.New
        |> setCommand commandArgList