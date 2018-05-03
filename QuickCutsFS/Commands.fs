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
open System.Collections.Generic
open ProgramArgs
open Result

module Commands =

    type NodeType =
        | Root
        | Inherit
        | Define
        | Profile
        | Template
        | Group
        | Command
        | Exec
        | Path

    type Node = {
        nodeType: NodeType;
        name: string;
        value: string option;
    }
    with override this.ToString() = sprintf "%s %s %O" (this.nodeType.ToString()) this.name this.value

    type CommandType =
        | Shortcut
        | Executable

    type Command = {
        name: string;
        cmdType: CommandType;
        execs: string list;
    }

    type TreeNode = TextTree.Node<Node>

    let (|Name|_|) (str:string) (arg:string) = 
        if System.String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0
        then Some() else None

    let namesEqual a b = System.StringComparer.OrdinalIgnoreCase.Equals(a, b)

    module Context =

        module Exceptions = 
            type NodeNotFound(name:string) =
                inherit Exception(sprintf "Node '%s' not found" name)
                let name = name

            type NodeWrongType(name:string, expectedType:NodeType) =
                inherit Exception(sprintf "Expected node '%s' to e of type '%O'" name expectedType)
                let name = name
                let expectedType = expectedType

        type Context = {
            nodes: TreeNode list
            branchNodes: TreeNode list
        }

        let private getMacroValue (key:String) (context:Context) =
            let getMacro name node =
                if node.nodeType = NodeType.Define then
                    if namesEqual node.name name then
                        match node.value with
                        | Some v -> Some v
                        | None -> Some ""
                    else None
                else None

            let rec getMacroInList name (nodes:TreeNode list) =
                match nodes with
                | head::tail ->
                    match getMacro name head.content with
                    | Some value -> Some value
                    | None -> getMacroInList name tail
                | [] -> None

            let result = getMacroInList key context.nodes
            result

        let expandValue (value:String) (context:Context) = Macros.expandValue value (fun s -> getMacroValue s context)
 
        let find (name:string) (context:Context) =
            let rec findInNodes (nodes:TreeNode list) (name:string) =
                match nodes with
                | head::tail ->
                    let headName = expandValue head.content.name context
                    if namesEqual headName name
                    then Some head
                    else findInNodes tail name
                | [] -> None

            expandValue name context
            |> findInNodes context.nodes

        let findChecked (name:string) (context:Context) =
            match find name context with
            | Some node -> node
            | None -> raise <| Exceptions.NodeNotFound(name)

        let findCheckedType (nodeType:NodeType) (name:string) (context:Context) =
            let node = findChecked name context
            if node.content.nodeType = nodeType
            then node
            else raise <| Exceptions.NodeWrongType(name, nodeType)

        let rec private addNode (node:TreeNode) (context:Context) =
            match node.content.nodeType with
            | Inherit -> inheritNode node.content.name context
            | _ -> { context with nodes = node :: context.nodes }

        and private addNodes (nodes:TreeNode list) (context:Context) =
            match nodes with
            | head::tail -> addNode head context |> addNodes tail
            | [] -> context

        and private inheritNode (name:string) (context:Context) =
            let baseNode = findChecked name context
            let branchNodes = List.append baseNode.children context.branchNodes
            let newContext = { nodes = context.nodes; branchNodes = branchNodes }
            let node = addNodes baseNode.children newContext
            node

        let addBranch (nodes:TreeNode list) (context:Context) =
            let newContext = { nodes = context.nodes; branchNodes = nodes }
            addNodes nodes newContext

        let create (nodes:TreeNode list) =
            addBranch nodes { nodes = []; branchNodes = [] }

    module Parse =

        module Exceptions = 
            type NodeMissingValue(name:string) =
                inherit Exception(sprintf "Node '%s' requires  value." name)
                let name = name

            type UnknownType(nodeType:string) =
                inherit Exception(sprintf "Unknown node type '%s'." nodeType)
                let nodeType = nodeType

        type MissingValue = { name:String; }
        type UnknownType = { name:String; }

        type Error =
            | MissingValue of MissingValue
            | UnknownType of UnknownType

        let isWhiteSpace c =
                match c with
                | ' ' | '\t' -> true
                | _ -> false

        let parseKeyValue (payload:string) =
            let keyCharCount = String.countChars (isWhiteSpace >> not) payload
            let key = payload |> String.subStringStart keyCharCount
            let value =
                payload
                |> String.subStringEnd keyCharCount
                |> String.trim
            match value with
                | "" -> (key, None)
                | _ -> (key, Some value)

        let parseNodeType key =
            match key with
            | Name "Inherit" -> Some (NodeType.Inherit)
            | Name "Define" -> Some (NodeType.Define)
            | Name "Profile" -> Some (NodeType.Profile)
            | Name "Template" -> Some (NodeType.Template)
            | Name "Group" -> Some (NodeType.Group)
            | Name "Command" -> Some (NodeType.Command)
            | Name "Exec" -> Some (NodeType.Exec)
            | Name "Path" -> Some (NodeType.Path)
            | _ -> None

        let createNodeWithValue nodeType name text =
            { nodeType = nodeType; name = name; value = Some text; }

        let createNodeWithKeyValue nodeType text =
            let key, value = parseKeyValue text
            { nodeType = nodeType; name = key; value = value; }

        let createNode key nodeType text =
            match text with
            | Some t ->
                match nodeType with
                | Exec -> createNodeWithValue nodeType "Exec" t
                | Path -> createNodeWithValue nodeType "Path" t
                | _ -> createNodeWithKeyValue nodeType t
            | None -> raise <| Exceptions.NodeMissingValue(key)

        let parseNode text =
            let key, value = parseKeyValue text
            let nodeTypeOption = parseNodeType key
            match nodeTypeOption with
            | Some nodeType -> createNode key nodeType value
            | None -> raise <| Exceptions.UnknownType(key)


    open Context

    let private addExec (node:Node) (context:Context) (execs:string list) =
        match node.value with
        | Some v ->
            let expanded = Context.expandValue v context
            expanded :: execs
        | None -> execs

    let rec private addCommandExecs (nodes:TreeNode list) (context:Context) (execs:string list) =
        match nodes with
        | head::tail ->
            match head.content.nodeType with
            | NodeType.Exec ->
                addExec head.content context execs
                |> addCommandExecs tail context
            | _ -> addCommandExecs tail context execs
        | [] -> execs

    let private buildCommand (node:TreeNode) (context:Context) =
        let name = Context.expandValue node.content.name context
        let inlineExecs = addExec node.content context []
        let branchContext = Context.addBranch node.children context
        let execs = addCommandExecs branchContext.branchNodes branchContext inlineExecs
        let command = { name=name; cmdType = Shortcut; execs=(List.rev execs) }
        command

    let commandFromFile path =
        let name = System.IO.Path.GetFileNameWithoutExtension(path)
        let command = { name=name; cmdType = Executable; execs=[path]}
        command

    let isExecutable path =
        match System.IO.Path.GetExtension(path) with
        | Name ".exe" -> true
        | Name ".bat" -> true
        | Name ".py" -> true
        | _ -> false

    let addCommandsFromDirectory (dir:string) (commands:Command list) = 
        System.IO.Directory.GetFiles(dir)
            |> Seq.filter isExecutable
            |> Seq.map commandFromFile
            |> Seq.toList
            |> List.append commands

    let addCommandsFromPath (path:string) (commands:Command list) = 
        try
            addCommandsFromDirectory (path) commands
        with
            | ex ->
                try
                    addCommandsFromDirectory (Environment.CurrentDirectory + path) commands
                with
                    | ex -> commands

    let rec private addCommandsFromNode (node:TreeNode) (context:Context) (commands:Command list) =
        match node.content.nodeType with
        | NodeType.Command ->
            let command = buildCommand node context
            command :: commands
        | NodeType.Group -> addCommandsFromBranch node context commands
        | NodeType.Path -> 
            match node.content.value with
            | Some v ->
                let path = String.trimChars v '"'
                addCommandsFromPath path commands
            | None -> commands
        | _ -> commands

    and private addCommandsFromList (nodes:TreeNode list) (context:Context) (commands:Command list) =
        match nodes with
        | head::tail ->
            addCommandsFromNode head context commands
            |> addCommandsFromList tail context
        | [] -> commands

    and private addCommandsFromBranch (node:TreeNode) (context:Context) (commands:Command list) =
        let branchContext = Context.addBranch node.children context
        let branchCommands = addCommandsFromList branchContext.branchNodes branchContext commands
        branchCommands

    let getCommandsForProfile (file:string) (profile:string) =
        let tree = TextTree.Parse.fromFile file Parse.parseNode
        let context = Context.create tree.children
        let profileNode = Context.findCheckedType NodeType.Profile profile context
        let commands = addCommandsFromBranch profileNode context []
        List.rev commands