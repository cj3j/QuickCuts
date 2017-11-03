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
open System.IO
open System.Collections.Generic
open Result
open Enumerator

module TextTree =

    type Source = {
        file: string;
        line: int;
    }

    type Node<'T> = {
        source: Source
        content: 'T
        children: Node<'T> list
    }

    type Root<'T> = {
        children: Node<'T> list;
    }

    module Parse =

        module Exceptions = 
            type Parse(source:Source, message:string, innerEx:Exception) =
                inherit Exception(sprintf "%s Line %d. %s" source.file source.line message, innerEx)
                let source = source

            type Whitespace(source:Source) =
                inherit Parse(source, "Invalid amount of leading whitespace.", null)

            type Convert(source:Source, innerEx:Exception) =
                inherit Parse(source, "Error converting text.", innerEx)

        type DepthLine = {
            source: Source
            text: string;
            depth: int;
        }

        type Converter<'T> = string -> 'T

        let private generateDepths source (sequence:seq<string>) =
            let removeLineComment str =
                let idxOpt = str |> String.indexOf "//"
                match idxOpt with
                | None -> str
                | Some idx -> str |> String.subStringStart idx |> String.trimEnd

            let isWhiteSpace c =
                match c with
                | ' ' | '\t' -> true
                | _ -> false

            let parseDepthLine (prevSource, prevLine) textLine =
                let newSource = { prevSource with line = prevSource.line + 1 }
                let payload = textLine |> removeLineComment |> String.trim
                let depth = textLine |> String.countChars isWhiteSpace
                match payload with
                    | "" -> (newSource, None)
                    | _ ->
                        let line = { source = newSource; text = payload; depth = depth }
                        (newSource, Some line)

            sequence
                |> Seq.scan parseDepthLine (source, None)
                |> Seq.map snd
                |> Seq.choose id
               
        let private generateTree (lineSeq:seq<DepthLine>) (convert:Converter<'T>) =
            let rec buildNode (line:DepthLine) (lines:DepthLine Enumerator) = 
                try
                let value = convert line.text
                let children, remaining = buildChildren line.depth lines
                let node = { source=line.source; content = value; children = List.rev children }
                (node, remaining)
                with
                | :? Exceptions.Parse as pex -> raise pex // TODO reraise
                | ex -> raise <| Exceptions.Parse(line.source, ex.Message, ex)            

            and buildChildren parentDepth lines =
                match lines.current with
                | None -> ([], lines)
                | Some line ->
                    if line.depth > parentDepth then
                        buildChildrenAtDepth line.depth [] lines
                    else
                        ([], lines)

            and buildChildrenAtDepth childDepth children lines =
                match lines.current with
                | None -> (children, lines)
                | Some line ->
                    if line.depth = childDepth then
                        let remaining = Enumerator.next lines
                        buildChild childDepth children line remaining
                    else if line.depth < childDepth then
                        (children, lines)
                    else
                        raise <| Exceptions.Whitespace(line.source)

            and buildChild childDepth children line lines =
                let node, remaining = buildNode line lines
                let newChildren, newRemaining = buildChildrenAtDepth childDepth (node::children) remaining
                (newChildren, newRemaining)

            let lines = Enumerator.make(lineSeq.GetEnumerator())
            let children, _ = buildChildrenAtDepth 0 [] lines
            let root = { children = List.rev children }
            root

        let fromSeq source sequence convert =
            let depthSeq = generateDepths source sequence
            generateTree depthSeq convert

        let fromFile (filePath:string) (convert:Converter<'T>) =
            let readLines = seq {
                use reader = new StreamReader(filePath)
                while not reader.EndOfStream do
                    yield reader.ReadLine()
            }

            let source = {file = filePath; line = 0;}

            try
                fromSeq source readLines convert
            with
                | :? Exceptions.Parse as ex -> raise ex // TODO reraise
                | ex -> raise <| Exceptions.Parse(source, sprintf "Error parsing file '%s'." filePath, ex)