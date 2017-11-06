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
open ProgramArgs

module Program =

    module Exceptions = 
        type MissingEndQuote(cmd:string) =
            inherit Exception(sprintf "Command '%s' is missing an end quote." cmd)

    [<EntryPoint>]
    let main argv = 
        let printExec (command:string) (exec:string) =
            printfn "%s %s" command exec

        let rec printExecs (command:string) (execs:string list) =
            match execs with
            | head::tail ->
                printExec command head
                printExecs command tail
            | [] -> ()

        let rec printCommands (commands:Commands.Command list) =
            match commands with
            | head::tail ->
                printExecs head.name head.execs
                printCommands tail
            | [] -> ()

        let getCommands args =
            let commands = Commands.getCommandsForProfile args.file args.profile
            commands

        let getExceptionMessage (ex:Exception) =
            let rec combineExceptionMessages (ex:Exception) message =
                if ex = null then message
                else combineExceptionMessages ex.InnerException (message + "\n" + ex.Message)
            combineExceptionMessages ex.InnerException ex.Message
            //ex.Message

        let parseQuotedProgramAndArgs exec:string : string*string =
            let quoteEndIdx = String.indexOfAt "\"" 1 exec
            match quoteEndIdx with
            | Some endIdx ->
                let prog =
                    exec
                    |> String.subStringMid 1 (endIdx - 1)
                    |> String.trim
                let args =
                    exec
                    |> String.subStringEnd (endIdx + 1)
                    |> String.trim
                (prog, args)
            | None -> raise <| Exceptions.MissingEndQuote(exec)

        let parseSpacedProgramAndArgs exec:string : string*string =
            let spaceIdx = String.indexOf " " exec
            match spaceIdx with
            | Some index ->
                let prog =
                    exec
                    |> String.subStringStart index
                    |> String.trim
                let args =
                    exec
                    |> String.subStringEnd (index + 1)
                    |> String.trim
                (prog, args)
            | None -> (exec, "")

        let parseProgramAndArgs exec:string : string*string =
            let quoteStartIdx = String.indexOf "\"" exec
            match quoteStartIdx with
            | Some 0 -> parseQuotedProgramAndArgs exec
            | Some idx -> parseSpacedProgramAndArgs exec
            | None -> parseSpacedProgramAndArgs exec
                
        let rec executeCommand (progArgs:ProgramArguments) (execs:string list) =
            match execs with
            | [] -> ()
            | head::tail ->
                let program, execArgs = parseProgramAndArgs head
                let userArgs = (String.join progArgs.args " ")
                let args =
                    match execArgs.Length with
                    | 0 -> userArgs
                    | _ -> execArgs + " " + userArgs
                System.Diagnostics.Process.Start(program, args) |> ignore
                System.Threading.Thread.Sleep(100)
                executeCommand progArgs tail

        try
            let argList = Array.toList argv
            let progArgs = ProgramArgs.parseArgs argList
            let commands = getCommands progArgs

            match progArgs.list  with
            | true ->
                printCommands commands
                0
            | false ->
                let command =
                    commands
                    |> List.find (fun c -> Commands.namesEqual c.name progArgs.command)
                executeCommand progArgs command.execs
                0
        with
            | ex ->
                eprintf "%s" (getExceptionMessage ex)
                1

                // Environment.UserInteractive resulted in hang when run from another process
//                match Environment.UserInteractive with
//                | true ->
//                    System.Console.ReadKey() |> ignore
//                    1
//                | false -> 1
